using System.Data;
using Microsoft.Data.Sqlite;
using OptimaJet.Workflow.Core.Entities;
using OptimaJet.Workflow.Core.Fault;

namespace OptimaJet.Workflow.SQLite.Models;

public class WorkflowForm : DbObject<WorkflowFormEntity>
{
    private const int CreateNewFormVersionMaxAttempts = 3;
    private const int CreateNewFormVersionRetryDelayMilliseconds = 50;

    public WorkflowForm(string schemaName, int commandTimeout) : base(schemaName, nameof(WorkflowForm), commandTimeout)
    {
        DBColumns.AddRange([
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Id), IsKey = true, Type = DbType.Guid },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Name) },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Version), Type = DbType.Int32 },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.CreationDate), Type = DbType.DateTime2 },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.UpdatedDate), Type = DbType.DateTime2 },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Definition) },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Lock), Type = DbType.Int32 },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.TenantId), Type = DbType.String }
        ]);
    }

    public async Task<List<string>> GetFormNamesAsync(SqliteConnection connection, string tenantId = null)
    {
        string query = $"""
                        SELECT DISTINCT [{nameof(WorkflowFormEntity.Name)}]
                        FROM {ObjectName}
                        WHERE {GetAccessibleTenantFilter("tenantId", tenantId)}
                        """;

        WorkflowFormEntity[] forms = await SelectAsync(connection, query, CreateAccessibleTenantParameters("tenantId", tenantId))
            .ConfigureAwait(false);
        return forms.Select(f => f.Name).ToList();
    }

    public async Task<WorkflowFormEntity> GetFormAsync(SqliteConnection connection, string name, int? version = null,
        string tenantId = null)
    {
        return await GetPreferredFormAsync(connection, transaction: null, name, version, tenantId).ConfigureAwait(false);
    }

    public async Task<List<int>> GetFormVersionsAsync(SqliteConnection connection, string name, string tenantId = null)
    {
        string query = $"""
                        SELECT DISTINCT [{nameof(WorkflowFormEntity.Version)}]
                        FROM {ObjectName}
                        WHERE [{nameof(WorkflowFormEntity.Name)}] = @name
                          AND {GetVersionScopeFilter("tenantId", "name", tenantId)}
                        """;

        var parameters = new List<SqliteParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name)
        };

        AddAccessibleTenantParameter(parameters, "tenantId", tenantId);

        WorkflowFormEntity[] forms = await SelectAsync(connection, query, parameters.ToArray()).ConfigureAwait(false);
        return forms.Select(f => f.Version).ToList();
    }

    public async Task<WorkflowFormEntity> CreateNewFormVersionAsync(SqliteConnection connection, DateTime creationDate, string name,
        string defaultDefinition, int? version = null, string tenantId = null)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }

        for (int attempt = 0; attempt < CreateNewFormVersionMaxAttempts; attempt++)
        {
            await using SqliteTransaction transaction = connection.BeginTransaction();
            try
            {
                WorkflowFormEntity latestInTenantScope =
                    await GetExactScopeLatestFormAsync(connection, transaction, name, tenantId).ConfigureAwait(false);

                int newVersion = latestInTenantScope != null ? latestInTenantScope.Version + 1 : 0;

                WorkflowFormEntity sourceForm = version is null
                    ? latestInTenantScope
                    : await GetExactScopeFormAsync(connection, transaction, name, version.Value, tenantId).ConfigureAwait(false);

                if (sourceForm is null && tenantId is not null && latestInTenantScope is null)
                {
                    sourceForm = version is null
                        ? await GetExactScopeLatestFormAsync(connection, transaction, name, tenantId: null)
                            .ConfigureAwait(false)
                        : await GetExactScopeFormAsync(connection, transaction, name, version.Value, tenantId: null)
                            .ConfigureAwait(false);
                }

                if (version is not null && sourceForm is null)
                {
                    transaction.Commit();
                    return null;
                }

                string definition = sourceForm?.Definition ?? defaultDefinition;
                var id = Guid.NewGuid();

                await InsertFormAsync(connection, transaction, id, name, newVersion, creationDate, definition, tenantId)
                    .ConfigureAwait(false);
                transaction.Commit();

                return new WorkflowFormEntity
                {
                    Id = id,
                    Name = name,
                    Version = newVersion,
                    CreationDate = creationDate,
                    UpdatedDate = creationDate,
                    Definition = definition,
                    Lock = 0,
                    TenantId = tenantId
                };
            }
            catch (PersistenceProviderQueryException ex) when (ex.IsDuplicateKeyException())
            {
                transaction.Rollback();

                if (attempt == CreateNewFormVersionMaxAttempts - 1)
                {
                    throw;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(CreateNewFormVersionRetryDelayMilliseconds * (attempt + 1)))
                    .ConfigureAwait(false);
            }
        }

        throw new Exception("Unable to create a new form version.");
    }

    public async Task<WorkflowFormEntity> CreateNewFormIfNotExistsAsync(SqliteConnection connection, DateTime creationDate, string name,
        string defaultDefinition, string tenantId = null)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }

        await using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            WorkflowFormEntity existingForm = await GetExactScopeFormAsync(connection, transaction, name, version: null, tenantId)
                .ConfigureAwait(false);

            if (existingForm != null)
            {
                transaction.Commit();
                return existingForm;
            }

            string definition = defaultDefinition;
            if (tenantId is not null)
            {
                WorkflowFormEntity sharedForm =
                    await GetExactScopeFormAsync(connection, transaction, name, version: null, tenantId: null)
                        .ConfigureAwait(false);
                definition = sharedForm?.Definition ?? defaultDefinition;
            }

            var id = Guid.NewGuid();
            await InsertFormAsync(connection, transaction, id, name, 0, creationDate, definition, tenantId).ConfigureAwait(false);
            transaction.Commit();

            return new WorkflowFormEntity
            {
                Id = id,
                Name = name,
                Version = 0,
                CreationDate = creationDate,
                UpdatedDate = creationDate,
                Definition = definition,
                Lock = 0,
                TenantId = tenantId
            };
        }
        catch (PersistenceProviderQueryException ex) when (ex.IsDuplicateKeyException())
        {
            transaction.Rollback();
        }

        WorkflowFormEntity form = await GetExactScopeFormAsync(connection, transaction: null, name, version: null, tenantId)
            .ConfigureAwait(false);
        return form ?? throw new Exception("Unable to create a new form.");
    }

    public async Task<int> UpdateFormAsync(SqliteConnection connection, string name, int version, long oldLock, long newLock,
        string definition, DateTime updatedDate, string tenantId = null)
    {
        string query = $"""
                        UPDATE {ObjectName}
                        SET [{nameof(WorkflowFormEntity.Definition)}]  = @definition,
                            [{nameof(WorkflowFormEntity.Lock)}]        = @newLock,
                            [{nameof(WorkflowFormEntity.UpdatedDate)}] = @date
                        WHERE [{nameof(WorkflowFormEntity.Name)}] = @name
                          AND [{nameof(WorkflowFormEntity.Version)}] = @version
                          AND [{nameof(WorkflowFormEntity.Lock)}] = @oldLock
                          AND {GetTenantFilter("tenantId", tenantId)}
                        """;

        var parameters = new List<SqliteParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Definition), "definition", definition),
            CreateParameter(nameof(WorkflowFormEntity.Lock), "oldLock", oldLock),
            CreateParameter(nameof(WorkflowFormEntity.Lock), "newLock", newLock),
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name),
            CreateParameter(nameof(WorkflowFormEntity.Version), "version", version),
            CreateParameter(nameof(WorkflowFormEntity.UpdatedDate), "date", ToDbValue(updatedDate, DbType.DateTime2))
        };

        AddTenantParameter(parameters, "tenantId", tenantId);

        return await ExecuteCommandNonQueryAsync(connection, query, parameters.ToArray()).ConfigureAwait(false);
    }

    public async Task DeleteFormVersionAsync(SqliteConnection connection, string name, int version, string tenantId = null)
    {
        string query = $"""
                        DELETE FROM {ObjectName}
                        WHERE [{nameof(WorkflowFormEntity.Name)}] = @name
                          AND [{nameof(WorkflowFormEntity.Version)}] = @version
                          AND {GetTenantFilter("tenantId", tenantId)}
                        """;

        var parameters = new List<SqliteParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name),
            CreateParameter(nameof(WorkflowFormEntity.Version), "version", version)
        };

        AddTenantParameter(parameters, "tenantId", tenantId);

        await ExecuteCommandNonQueryAsync(connection, query, parameters.ToArray()).ConfigureAwait(false);
    }

    public async Task DeleteFormAsync(SqliteConnection connection, string name, string tenantId = null)
    {
        string query = $"""
                        DELETE FROM {ObjectName}
                        WHERE [{nameof(WorkflowFormEntity.Name)}] = @name
                          AND {GetTenantFilter("tenantId", tenantId)}
                        """;

        var parameters = new List<SqliteParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name)
        };

        AddTenantParameter(parameters, "tenantId", tenantId);

        await ExecuteCommandNonQueryAsync(connection, query, parameters.ToArray()).ConfigureAwait(false);
    }

    private async Task<WorkflowFormEntity> GetPreferredFormAsync(SqliteConnection connection, SqliteTransaction transaction, string name,
        int? version, string tenantId)
    {
        string query = version is null
            ? $"""
               SELECT *
               FROM {ObjectName}
               WHERE [{nameof(WorkflowFormEntity.Name)}] = @name
                 AND {GetAccessibleTenantFilter("tenantId", tenantId)}
               ORDER BY {GetPreferredScopeOrder("tenantId", tenantId, includeVersionOrder: true)}
               LIMIT 1
               """
            : $"""
               SELECT *
               FROM {ObjectName}
               WHERE [{nameof(WorkflowFormEntity.Name)}] = @name
                 AND [{nameof(WorkflowFormEntity.Version)}] = @version
                 AND {GetVersionScopeFilter("tenantId", "name", tenantId)}
               ORDER BY {GetPreferredScopeOrder("tenantId", tenantId, includeVersionOrder: false)}
               LIMIT 1
               """;

        var parameters = new List<SqliteParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name)
        };

        if (version.HasValue)
        {
            parameters.Add(CreateParameter(nameof(WorkflowFormEntity.Version), "version", version.Value));
        }

        AddAccessibleTenantParameter(parameters, "tenantId", tenantId);

        WorkflowFormEntity[] forms = transaction is null
            ? await SelectAsync(connection, query, parameters.ToArray()).ConfigureAwait(false)
            : await SelectAsync(connection, query, transaction, parameters.ToArray()).ConfigureAwait(false);

        return forms.FirstOrDefault();
    }

    private Task<WorkflowFormEntity> GetExactScopeLatestFormAsync(SqliteConnection connection, SqliteTransaction transaction, string name,
        string tenantId)
    {
        return GetExactScopeFormAsync(connection, transaction, name, version: null, tenantId);
    }

    private async Task<WorkflowFormEntity> GetExactScopeFormAsync(SqliteConnection connection, SqliteTransaction transaction, string name,
        int? version, string tenantId)
    {
        string query = version is null
            ? $"""
               SELECT *
               FROM {ObjectName}
               WHERE [{nameof(WorkflowFormEntity.Name)}] = @name
                 AND {GetTenantFilter("tenantId", tenantId)}
               ORDER BY [{nameof(WorkflowFormEntity.Version)}] DESC
               LIMIT 1
               """
            : $"""
               SELECT *
               FROM {ObjectName}
               WHERE [{nameof(WorkflowFormEntity.Name)}] = @name
                 AND [{nameof(WorkflowFormEntity.Version)}] = @version
                 AND {GetTenantFilter("tenantId", tenantId)}
               LIMIT 1
               """;

        var parameters = new List<SqliteParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name)
        };

        if (version.HasValue)
        {
            parameters.Add(CreateParameter(nameof(WorkflowFormEntity.Version), "version", version.Value));
        }

        AddTenantParameter(parameters, "tenantId", tenantId);

        WorkflowFormEntity[] forms = transaction is null
            ? await SelectAsync(connection, query, parameters.ToArray()).ConfigureAwait(false)
            : await SelectAsync(connection, query, transaction, parameters.ToArray()).ConfigureAwait(false);

        return forms.FirstOrDefault();
    }

    private async Task InsertFormAsync(SqliteConnection connection, SqliteTransaction transaction, Guid id, string name, int version,
        DateTime creationDate, string definition, string tenantId)
    {
        string insertQuery = $"""
                              INSERT INTO {ObjectName} ([Id], [{nameof(WorkflowFormEntity.Name)}], [{nameof(WorkflowFormEntity.Version)}],
                              [{nameof(WorkflowFormEntity.CreationDate)}], [{nameof(WorkflowFormEntity.UpdatedDate)}],
                              [{nameof(WorkflowFormEntity.Definition)}], [{nameof(WorkflowFormEntity.Lock)}],
                              [{nameof(WorkflowFormEntity.TenantId)}])
                              VALUES (@id, @name, @version, @date, @date, @definition, 0, @tenantId);
                              """;
        
        SqliteParameter[] parameters =
        [
            CreateParameter(nameof(WorkflowFormEntity.Id), "id", ToDbValue(id, DbType.Guid)),
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name),
            CreateParameter(nameof(WorkflowFormEntity.Version), "version", version),
            CreateParameter(nameof(WorkflowFormEntity.CreationDate), "date", ToDbValue(creationDate, DbType.DateTime2)),
            CreateParameter(nameof(WorkflowFormEntity.Definition), "definition", definition),
            CreateParameter(nameof(WorkflowFormEntity.TenantId), "tenantId", tenantId ?? (object)DBNull.Value)
        ];

        int inserted = await ExecuteCommandNonQueryAsync(connection, insertQuery, transaction, parameters).ConfigureAwait(false);

        if (inserted != 1)
        {
            throw new Exception($"There was an error inserting {ObjectName} to the database");
        }
    }

    private string GetTenantFilter(string parameterName, string tenantId)
    {
        return tenantId is null
            ? $"[{nameof(WorkflowFormEntity.TenantId)}] IS NULL"
            : $"[{nameof(WorkflowFormEntity.TenantId)}] = @{parameterName}";
    }

    private string GetAccessibleTenantFilter(string parameterName, string tenantId)
    {
        return tenantId is null
            ? $"[{nameof(WorkflowFormEntity.TenantId)}] IS NULL"
            : $"([{nameof(WorkflowFormEntity.TenantId)}] = @{parameterName} OR [{nameof(WorkflowFormEntity.TenantId)}] IS NULL)";
    }

    private string GetVersionScopeFilter(string tenantParameterName, string nameParameterName, string tenantId)
    {
        return tenantId is null
            ? $"[{nameof(WorkflowFormEntity.TenantId)}] IS NULL"
            : $"""
               (
                   [{nameof(WorkflowFormEntity.TenantId)}] = @{tenantParameterName}
                   OR (
                       [{nameof(WorkflowFormEntity.TenantId)}] IS NULL
                       AND NOT EXISTS (
                           SELECT 1
                           FROM {ObjectName} [TenantScope]
                           WHERE [TenantScope].[{nameof(WorkflowFormEntity.Name)}] = @{nameParameterName}
                             AND [TenantScope].[{nameof(WorkflowFormEntity.TenantId)}] = @{tenantParameterName}
                       )
                   )
               )
               """;
    }

    private string GetPreferredScopeOrder(string parameterName, string tenantId, bool includeVersionOrder)
    {
        return tenantId is null
            ? $"[{nameof(WorkflowFormEntity.Version)}] DESC"
            : includeVersionOrder
                ? $"CASE WHEN [{nameof(WorkflowFormEntity.TenantId)}] = @{parameterName} THEN 0 ELSE 1 END, [{nameof(WorkflowFormEntity.Version)}] DESC"
                : $"CASE WHEN [{nameof(WorkflowFormEntity.TenantId)}] = @{parameterName} THEN 0 ELSE 1 END";
    }

    private void AddTenantParameter(List<SqliteParameter> parameters, string parameterName, string tenantId)
    {
        if (tenantId is not null)
        {
            parameters.Add(CreateParameter(nameof(WorkflowFormEntity.TenantId), parameterName, tenantId));
        }
    }

    private SqliteParameter[] CreateTenantParameters(string parameterName, string tenantId)
    {
        return tenantId is null
            ? []
            : [CreateParameter(nameof(WorkflowFormEntity.TenantId), parameterName, tenantId)];
    }

    private void AddAccessibleTenantParameter(List<SqliteParameter> parameters, string parameterName, string tenantId)
    {
        if (tenantId is not null)
        {
            AddTenantParameter(parameters, parameterName, tenantId);
        }
    }

    private SqliteParameter[] CreateAccessibleTenantParameters(string parameterName, string tenantId)
    {
        return tenantId is null ? [] : CreateTenantParameters(parameterName, tenantId);
    }

    private SqliteParameter CreateParameter(string columnName, string parameterName, object value)
    {
        DbType type = DBColumns.Find(c => c.Name == columnName).Type;
        return new SqliteParameter(parameterName, type) { Value = value };
    }
}
