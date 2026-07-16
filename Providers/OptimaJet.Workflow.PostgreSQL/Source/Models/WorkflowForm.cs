using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using OptimaJet.Workflow.Core.Entities;
using OptimaJet.Workflow.PostgreSQL;

namespace OptimaJet.Workflow.PostgreSQL.Models;

public class WorkflowForm : DbObject<WorkflowFormEntity>
{
    private const int CreateNewFormVersionMaxAttempts = 3;
    private const int CreateNewFormVersionRetryDelayMilliseconds = 50;

    public WorkflowForm(string schemaName, int commandTimeout) : base(schemaName, nameof(WorkflowForm), commandTimeout)
    {
        DBColumns.AddRange([
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Id), IsKey = true, Type = NpgsqlDbType.Uuid },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Name) },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Version), Type = NpgsqlDbType.Integer },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.CreationDate), Type = NpgsqlDbType.Timestamp },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.UpdatedDate), Type = NpgsqlDbType.Timestamp },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Definition) },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Lock), Type = NpgsqlDbType.Integer },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.TenantId), Type = NpgsqlDbType.Varchar }
        ]);
    }

    public async Task<List<string>> GetFormNamesAsync(NpgsqlConnection connection, string tenantId = null)
    {
        string query = $"""
                        SELECT DISTINCT "{nameof(WorkflowFormEntity.Name)}"
                        FROM {ObjectName}
                        WHERE {GetAccessibleTenantFilter("tenantId", tenantId)}
                        """;

        WorkflowFormEntity[] result = await SelectAsync(connection, query, CreateAccessibleTenantParameters("tenantId", tenantId))
            .ConfigureAwait(false);
        return result.Select(f => f.Name).ToList();
    }

    public async Task<WorkflowFormEntity> GetFormAsync(NpgsqlConnection connection, string name, int? version = null,
        string tenantId = null)
    {
        return await GetPreferredFormAsync(connection, transaction: null, name, version, tenantId).ConfigureAwait(false);
    }

    public async Task<List<int>> GetFormVersionsAsync(NpgsqlConnection connection, string name, string tenantId = null)
    {
        string query = $"""
                        SELECT DISTINCT "{nameof(WorkflowFormEntity.Version)}"
                        FROM {ObjectName}
                        WHERE "{nameof(WorkflowFormEntity.Name)}" = @name
                          AND {GetVersionScopeFilter("tenantId", "name", tenantId)}
                        """;

        var parameters = new List<NpgsqlParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name)
        };

        AddAccessibleTenantParameter(parameters, "tenantId", tenantId);

        WorkflowFormEntity[] forms = await SelectAsync(connection, query, parameters.ToArray()).ConfigureAwait(false);
        return forms.Select(f => f.Version).ToList();
    }

    public async Task<WorkflowFormEntity> CreateNewFormVersionAsync(NpgsqlConnection connection, DateTime creationDate, string name,
        string defaultDefinition, int? version = null, string tenantId = null)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }

        for (int attempt = 0; attempt < CreateNewFormVersionMaxAttempts; attempt++)
        {
            await using NpgsqlTransaction transaction = connection.BeginTransaction();
            try
            {
                WorkflowFormEntity latestInTenantScope =
                    await GetExactScopeLatestFormAsync(connection, transaction, name, tenantId)
                        .ConfigureAwait(false);

                int newVersion = latestInTenantScope != null ? latestInTenantScope.Version + 1 : 0;

                WorkflowFormEntity sourceForm = version is null
                    ? latestInTenantScope
                    : await GetExactScopeFormAsync(connection, transaction, name, version.Value, tenantId)
                        .ConfigureAwait(false);

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
                    await transaction.CommitAsync().ConfigureAwait(false);
                    return null;
                }

                string definition = sourceForm?.Definition ?? defaultDefinition;

                WorkflowFormEntity createdForm = await InsertFormAsync(connection, transaction, Guid.NewGuid(), name, newVersion,
                    creationDate, definition, tenantId).ConfigureAwait(false);

                await transaction.CommitAsync().ConfigureAwait(false);
                return createdForm;
            }
            catch (PostgresException ex) when (ex.IsDuplicateKeyException())
            {
                await transaction.RollbackAsync().ConfigureAwait(false);

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

    public async Task<WorkflowFormEntity> CreateNewFormIfNotExistsAsync(NpgsqlConnection connection, DateTime creationDate, string name,
        string defaultDefinition, string tenantId = null)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }

        await using NpgsqlTransaction transaction = connection.BeginTransaction();
        try
        {
            WorkflowFormEntity existingForm = await GetExactScopeFormAsync(connection, transaction, name, version: null, tenantId)
                .ConfigureAwait(false);

            if (existingForm != null)
            {
                await transaction.CommitAsync().ConfigureAwait(false);
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

            WorkflowFormEntity createdForm = await InsertFormAsync(connection, transaction, Guid.NewGuid(), name, 0, creationDate,
                definition, tenantId).ConfigureAwait(false);

            await transaction.CommitAsync().ConfigureAwait(false);
            return createdForm;
        }
        catch (PostgresException ex) when (ex.IsDuplicateKeyException())
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
        }

        WorkflowFormEntity form =
            await GetExactScopeFormAsync(connection, transaction: null, name, version: null, tenantId)
                .ConfigureAwait(false);
        return form ?? throw new Exception("Unable to create a new form.");
    }

    public async Task<int> UpdateFormAsync(NpgsqlConnection connection, string name, int version, long oldLock, long newLock,
        string definition, DateTime updatedDate, string tenantId = null)
    {
        string query = $"""
                        UPDATE {ObjectName}
                        SET "{nameof(WorkflowFormEntity.Definition)}"  = @definition,
                            "{nameof(WorkflowFormEntity.Lock)}"        = @newLock,
                            "{nameof(WorkflowFormEntity.UpdatedDate)}" = @date
                        WHERE "{nameof(WorkflowFormEntity.Name)}" = @name
                          AND "{nameof(WorkflowFormEntity.Version)}" = @version
                          AND "{nameof(WorkflowFormEntity.Lock)}" = @oldLock
                          AND {GetTenantFilter("tenantId", tenantId)}
                        """;

        var parameters = new List<NpgsqlParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Definition), "definition", definition),
            CreateParameter(nameof(WorkflowFormEntity.Lock), "oldLock", oldLock),
            CreateParameter(nameof(WorkflowFormEntity.Lock), "newLock", newLock),
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name),
            CreateParameter(nameof(WorkflowFormEntity.Version), "version", version),
            CreateParameter(nameof(WorkflowFormEntity.UpdatedDate), "date", updatedDate)
        };

        AddTenantParameter(parameters, "tenantId", tenantId);

        return await ExecuteCommandNonQueryAsync(connection, query, parameters.ToArray()).ConfigureAwait(false);
    }

    public async Task DeleteFormVersionAsync(NpgsqlConnection connection, string name, int version, string tenantId = null)
    {
        string query = $"""
                        DELETE FROM {ObjectName}
                        WHERE "{nameof(WorkflowFormEntity.Name)}" = @name
                          AND "{nameof(WorkflowFormEntity.Version)}" = @version
                          AND {GetTenantFilter("tenantId", tenantId)}
                        """;

        var parameters = new List<NpgsqlParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name),
            CreateParameter(nameof(WorkflowFormEntity.Version), "version", version)
        };

        AddTenantParameter(parameters, "tenantId", tenantId);

        await ExecuteCommandNonQueryAsync(connection, query, parameters.ToArray()).ConfigureAwait(false);
    }

    public async Task DeleteFormAsync(NpgsqlConnection connection, string name, string tenantId = null)
    {
        string query = $"""
                        DELETE FROM {ObjectName}
                        WHERE "{nameof(WorkflowFormEntity.Name)}" = @name
                          AND {GetTenantFilter("tenantId", tenantId)}
                        """;

        var parameters = new List<NpgsqlParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name)
        };

        AddTenantParameter(parameters, "tenantId", tenantId);

        await ExecuteCommandNonQueryAsync(connection, query, parameters.ToArray()).ConfigureAwait(false);
    }

    private async Task<WorkflowFormEntity> GetPreferredFormAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        string name, int? version, string tenantId)
    {
        string query = version is null
            ? $"""
               SELECT *
               FROM {ObjectName}
               WHERE "{nameof(WorkflowFormEntity.Name)}" = @name
                 AND {GetAccessibleTenantFilter("tenantId", tenantId)}
               ORDER BY {GetPreferredScopeOrder("tenantId", tenantId, includeVersionOrder: true)}
               LIMIT 1
               """
            : $"""
               SELECT *
               FROM {ObjectName}
               WHERE "{nameof(WorkflowFormEntity.Name)}" = @name
                 AND "{nameof(WorkflowFormEntity.Version)}" = @version
                 AND {GetVersionScopeFilter("tenantId", "name", tenantId)}
               ORDER BY {GetPreferredScopeOrder("tenantId", tenantId, includeVersionOrder: false)}
               LIMIT 1
               """;

        var parameters = new List<NpgsqlParameter>
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

    private Task<WorkflowFormEntity> GetExactScopeLatestFormAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string name,
        string tenantId)
    {
        return GetExactScopeFormAsync(connection, transaction, name, version: null, tenantId);
    }

    private async Task<WorkflowFormEntity> GetExactScopeFormAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string name,
        int? version, string tenantId)
    {
        string query = version is null
            ? $"""
               SELECT *
               FROM {ObjectName}
               WHERE "{nameof(WorkflowFormEntity.Name)}" = @name
                 AND {GetTenantFilter("tenantId", tenantId)}
               ORDER BY "{nameof(WorkflowFormEntity.Version)}" DESC
               LIMIT 1
               """
            : $"""
               SELECT *
               FROM {ObjectName}
               WHERE "{nameof(WorkflowFormEntity.Name)}" = @name
                 AND "{nameof(WorkflowFormEntity.Version)}" = @version
                 AND {GetTenantFilter("tenantId", tenantId)}
               """;

        var parameters = new List<NpgsqlParameter>
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

    private async Task<WorkflowFormEntity> InsertFormAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid id, string name,
        int version, DateTime creationDate, string definition, string tenantId)
    {
        string query = $"""
                        INSERT INTO {ObjectName} ("{nameof(WorkflowFormEntity.Id)}", "{nameof(WorkflowFormEntity.Name)}",
                        "{nameof(WorkflowFormEntity.Version)}", "{nameof(WorkflowFormEntity.CreationDate)}",
                        "{nameof(WorkflowFormEntity.UpdatedDate)}", "{nameof(WorkflowFormEntity.Definition)}",
                        "{nameof(WorkflowFormEntity.Lock)}", "{nameof(WorkflowFormEntity.TenantId)}")
                        VALUES (@id, @name, @version, @creationDate, @creationDate, @definition, 0, @tenantId)
                        RETURNING *;
                        """;

        NpgsqlParameter[] parameters =
        [
            CreateParameter(nameof(WorkflowFormEntity.Id), "id", id),
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name),
            CreateParameter(nameof(WorkflowFormEntity.Version), "version", version),
            CreateParameter(nameof(WorkflowFormEntity.CreationDate), "creationDate", creationDate),
            CreateParameter(nameof(WorkflowFormEntity.Definition), "definition", definition),
            CreateParameter(nameof(WorkflowFormEntity.TenantId), "tenantId", tenantId ?? (object)DBNull.Value)
        ];

        WorkflowFormEntity[] result = await SelectAsync(connection, query, transaction, parameters).ConfigureAwait(false);

        if (result.Length != 1)
        {
            throw new Exception($"There was an error inserting {ObjectName} to the database");
        }

        return result[0];
    }

    private string GetTenantFilter(string parameterName, string tenantId)
    {
        return tenantId is null
            ? $"\"{nameof(WorkflowFormEntity.TenantId)}\" IS NULL"
            : $"\"{nameof(WorkflowFormEntity.TenantId)}\" = @{parameterName}";
    }

    private string GetAccessibleTenantFilter(string parameterName, string tenantId)
    {
        return tenantId is null
            ? $"\"{nameof(WorkflowFormEntity.TenantId)}\" IS NULL"
            : $"(\"{nameof(WorkflowFormEntity.TenantId)}\" = @{parameterName} OR \"{nameof(WorkflowFormEntity.TenantId)}\" IS NULL)";
    }

    private string GetVersionScopeFilter(string tenantParameterName, string nameParameterName, string tenantId)
    {
        return tenantId is null
            ? $"\"{nameof(WorkflowFormEntity.TenantId)}\" IS NULL"
            : $"""
               (
                   "{nameof(WorkflowFormEntity.TenantId)}" = @{tenantParameterName}
                   OR (
                       "{nameof(WorkflowFormEntity.TenantId)}" IS NULL
                       AND NOT EXISTS (
                           SELECT 1
                           FROM {ObjectName} "TenantScope"
                           WHERE "TenantScope"."{nameof(WorkflowFormEntity.Name)}" = @{nameParameterName}
                             AND "TenantScope"."{nameof(WorkflowFormEntity.TenantId)}" = @{tenantParameterName}
                       )
                   )
               )
               """;
    }

    private string GetPreferredScopeOrder(string parameterName, string tenantId, bool includeVersionOrder)
    {
        return tenantId is null
            ? $"\"{nameof(WorkflowFormEntity.Version)}\" DESC"
            : includeVersionOrder
                ? $"CASE WHEN \"{nameof(WorkflowFormEntity.TenantId)}\" = @{parameterName} THEN 0 ELSE 1 END, \"{nameof(WorkflowFormEntity.Version)}\" DESC"
                : $"CASE WHEN \"{nameof(WorkflowFormEntity.TenantId)}\" = @{parameterName} THEN 0 ELSE 1 END";
    }

    private void AddTenantParameter(List<NpgsqlParameter> parameters, string parameterName, string tenantId)
    {
        if (tenantId is not null)
        {
            parameters.Add(CreateParameter(nameof(WorkflowFormEntity.TenantId), parameterName, tenantId));
        }
    }

    private NpgsqlParameter[] CreateTenantParameters(string parameterName, string tenantId)
    {
        return tenantId is null
            ? []
            : [CreateParameter(nameof(WorkflowFormEntity.TenantId), parameterName, tenantId)];
    }

    private void AddAccessibleTenantParameter(List<NpgsqlParameter> parameters, string parameterName, string tenantId)
    {
        if (tenantId is not null)
        {
            AddTenantParameter(parameters, parameterName, tenantId);
        }
    }

    private NpgsqlParameter[] CreateAccessibleTenantParameters(string parameterName, string tenantId)
    {
        return tenantId is null ? [] : CreateTenantParameters(parameterName, tenantId);
    }

    private NpgsqlParameter CreateParameter(string columnName, string parameterName, object value)
    {
        NpgsqlDbType type = DBColumns.Find(c => c.Name == columnName).Type;
        return new NpgsqlParameter(parameterName, type) { Value = value };
    }
}
