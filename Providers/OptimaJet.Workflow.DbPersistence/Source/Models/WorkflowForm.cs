using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using OptimaJet.Workflow.Core.Entities;
using OptimaJet.Workflow.Core.Fault;
using OptimaJet.Workflow.DbPersistence;

namespace OptimaJet.Workflow.MSSQL.Models;

public class WorkflowForm : DbObject<WorkflowFormEntity>
{
    private const int CreateNewFormVersionMaxAttempts = 3;
    private const int CreateNewFormVersionRetryDelayMilliseconds = 50;

    public WorkflowForm(string schemaName, int commandTimeout) : base(schemaName, nameof(WorkflowForm), commandTimeout)
    {
        DBColumns.AddRange([
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Id), IsKey = true, Type = SqlDbType.UniqueIdentifier },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Name), Size = 512 },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Version), Type = SqlDbType.Int },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.CreationDate), Type = SqlDbType.DateTime },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.UpdatedDate), Type = SqlDbType.DateTime },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Definition), Size = -1 },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Lock), Type = SqlDbType.Int },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.TenantId), Type = SqlDbType.NVarChar, Size = 128 }
        ]);
    }

    public async Task<List<string>> GetFormNamesAsync(SqlConnection connection, string tenantId = null)
    {
        string selectText = $"""
                            SELECT DISTINCT [Name]
                            FROM {ObjectName}
                            WHERE {GetAccessibleTenantFilter("TenantId", tenantId)}
                            """;

        WorkflowFormEntity[] formNames = await SelectAsync(connection, selectText, CreateAccessibleTenantParameters("TenantId", tenantId))
            .ConfigureAwait(false);

        return formNames.Select(f => f.Name).ToList();
    }

    public async Task<WorkflowFormEntity> GetFormAsync(SqlConnection connection, string name, int? version = null,
        string tenantId = null)
    {
        return await GetPreferredFormAsync(connection, name, version, tenantId).ConfigureAwait(false);
    }

    public async Task<List<int>> GetFormVersionsAsync(SqlConnection connection, string name, string tenantId = null)
    {
        string selectText = $"""
                            SELECT DISTINCT [Version]
                            FROM {ObjectName}
                            WHERE [Name] = @Name
                              AND {GetVersionScopeFilter("TenantId", "Name", tenantId)}
                            """;

        var parameters = new List<SqlParameter>
        {
            new("Name", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Name)).Type) { Value = name }
        };

        AddAccessibleTenantParameter(parameters, "TenantId", tenantId);

        WorkflowFormEntity[] workflowForms = await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false);
        return workflowForms.Select(f => f.Version).ToList();
    }

    public async Task<WorkflowFormEntity> CreateNewFormVersionAsync(SqlConnection connection, DateTime creationDate, string name,
        string defaultDefinition, int? version = null, string tenantId = null)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }

        for (int attempt = 0; attempt < CreateNewFormVersionMaxAttempts; attempt++)
        {
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            try
            {
                WorkflowFormEntity latestInTenantScope = await GetExactScopeFormAsync(connection, transaction, name, version: null,
                        tenantId)
                    .ConfigureAwait(false);
                int newVersion = latestInTenantScope != null ? latestInTenantScope.Version + 1 : 0;

                WorkflowFormEntity sourceForm = version is null
                    ? latestInTenantScope
                    : await GetExactScopeFormAsync(connection, transaction, name, version.Value, tenantId)
                        .ConfigureAwait(false);

                if (sourceForm is null && tenantId is not null && latestInTenantScope is null)
                {
                    sourceForm = version is null
                        ? await GetExactScopeFormAsync(connection, transaction, name, version: null, tenantId: null)
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
                WorkflowFormEntity createdForm =
                    await InsertFormAsync(connection, transaction, Guid.NewGuid(), name, newVersion, creationDate, definition, tenantId)
                        .ConfigureAwait(false);

                transaction.Commit();
                return createdForm;
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

    public async Task<WorkflowFormEntity> CreateNewFormIfNotExistsAsync(SqlConnection connection, DateTime creationDate, string name,
        string defaultDefinition, string tenantId = null)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }

        using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
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
                WorkflowFormEntity sharedForm = await GetExactScopeFormAsync(connection, transaction, name, version: null,
                        tenantId: null)
                    .ConfigureAwait(false);
                definition = sharedForm?.Definition ?? defaultDefinition;
            }

            WorkflowFormEntity createdForm =
                await InsertFormAsync(connection, transaction, Guid.NewGuid(), name, 0, creationDate, definition, tenantId)
                    .ConfigureAwait(false);

            transaction.Commit();
            return createdForm;
        }
        catch (PersistenceProviderQueryException ex) when (ex.IsDuplicateKeyException())
        {
            transaction.Rollback();
        }

        WorkflowFormEntity form = await GetExactScopeFormAsync(connection, transaction: null, name, version: null, tenantId)
            .ConfigureAwait(false);
        return form ?? throw new Exception("Unable to create a new form.");
    }

    public async Task<int> UpdateFormAsync(SqlConnection connection, string name, int version, long oldLock, long newLock,
        string definition, DateTime updatedDate, string tenantId = null)
    {
        string commandText = $"""
                              UPDATE {ObjectName}
                              SET [Definition]  = @Definition,
                                  [Lock]        = @NewLock,
                                  [UpdatedDate] = @Date
                              WHERE [Name] = @Name
                                AND [Version] = @Version
                                AND [Lock] = @OldLock
                                AND {GetTenantFilter("TenantId", tenantId)}
                              """;

        var parameters = new List<SqlParameter>
        {
            new("Definition", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Definition)).Type) { Value = definition },
            new("OldLock", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Lock)).Type) { Value = oldLock },
            new("NewLock", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Lock)).Type) { Value = newLock },
            new("Name", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Name)).Type) { Value = name },
            new("Version", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Version)).Type) { Value = version },
            new("Date", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.UpdatedDate)).Type) { Value = updatedDate }
        };

        AddTenantParameter(parameters, "TenantId", tenantId);

        return await ExecuteCommandNonQueryAsync(connection, commandText, parameters.ToArray()).ConfigureAwait(false);
    }

    public async Task DeleteFormVersionAsync(SqlConnection connection, string name, int version, string tenantId = null)
    {
        string commandText = $"""
                              DELETE FROM {ObjectName}
                              WHERE [Name] = @Name
                                AND [Version] = @Version
                                AND {GetTenantFilter("TenantId", tenantId)}
                              """;

        var parameters = new List<SqlParameter>
        {
            new("Name", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Name)).Type) { Value = name },
            new("Version", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Version)).Type) { Value = version }
        };

        AddTenantParameter(parameters, "TenantId", tenantId);

        await ExecuteCommandNonQueryAsync(connection, commandText, parameters.ToArray()).ConfigureAwait(false);
    }

    public async Task DeleteFormAsync(SqlConnection connection, string name, string tenantId = null)
    {
        string commandText = $"""
                              DELETE FROM {ObjectName}
                              WHERE [Name] = @Name
                                AND {GetTenantFilter("TenantId", tenantId)}
                              """;

        var parameters = new List<SqlParameter>
        {
            new("Name", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Name)).Type) { Value = name }
        };

        AddTenantParameter(parameters, "TenantId", tenantId);

        await ExecuteCommandNonQueryAsync(connection, commandText, parameters.ToArray()).ConfigureAwait(false);
    }

    private async Task<WorkflowFormEntity> GetExactScopeFormAsync(SqlConnection connection, SqlTransaction transaction, string name,
        int? version, string tenantId)
    {
        string selectText = version is null
            ? $"""
               SELECT TOP 1 *
               FROM {ObjectName}
               WHERE [Name] = @Name
                 AND {GetTenantFilter("TenantId", tenantId)}
               ORDER BY [Version] DESC
               """
            : $"""
               SELECT *
               FROM {ObjectName}
               WHERE [Name] = @Name
                 AND [Version] = @Version
                 AND {GetTenantFilter("TenantId", tenantId)}
               """;

        var parameters = new List<SqlParameter>
        {
            new("Name", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Name)).Type) { Value = name }
        };

        if (version.HasValue)
        {
            parameters.Add(new("Version", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Version)).Type)
                { Value = version.Value });
        }

        AddTenantParameter(parameters, "TenantId", tenantId);

        WorkflowFormEntity[] workflowForms = transaction is null
            ? await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false)
            : await SelectAsync(connection, selectText, transaction, parameters.ToArray()).ConfigureAwait(false);
        return workflowForms.FirstOrDefault();
    }

    private Task<WorkflowFormEntity> GetPreferredFormAsync(SqlConnection connection, string name, int? version, string tenantId)
    {
        return GetPreferredFormAsync(connection, transaction: null, name, version, tenantId);
    }

    private async Task<WorkflowFormEntity> GetPreferredFormAsync(SqlConnection connection, SqlTransaction transaction, string name,
        int? version, string tenantId)
    {
        string selectText = version is null
            ? $"""
               SELECT TOP 1 *
               FROM {ObjectName}
               WHERE [Name] = @Name
                 AND {GetAccessibleTenantFilter("TenantId", tenantId)}
               ORDER BY {GetPreferredScopeOrder("TenantId", tenantId, includeVersionOrder: true)}
               """
            : $"""
               SELECT TOP 1 *
               FROM {ObjectName}
               WHERE [Name] = @Name
                 AND [Version] = @Version
                 AND {GetVersionScopeFilter("TenantId", "Name", tenantId)}
               ORDER BY {GetPreferredScopeOrder("TenantId", tenantId, includeVersionOrder: false)}
               """;

        var parameters = new List<SqlParameter>
        {
            new("Name", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Name)).Type) { Value = name }
        };

        if (version.HasValue)
        {
            parameters.Add(new("Version", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Version)).Type)
                { Value = version.Value });
        }

        AddAccessibleTenantParameter(parameters, "TenantId", tenantId);

        WorkflowFormEntity[] workflowForms = transaction is null
            ? await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false)
            : await SelectAsync(connection, selectText, transaction, parameters.ToArray()).ConfigureAwait(false);
        return workflowForms.FirstOrDefault();
    }

    private async Task<WorkflowFormEntity> InsertFormAsync(SqlConnection connection, SqlTransaction transaction, Guid id, string name,
        int version, DateTime creationDate, string definition, string tenantId)
    {
        string commandText = $"""
                             INSERT INTO {ObjectName} ([Id], [Name], [Version], [CreationDate], [UpdatedDate], [Definition], [Lock], [TenantId])
                             OUTPUT INSERTED.*
                             VALUES (@Id, @Name, @Version, @Date, @Date, @Definition, 0, @TenantId)
                             """;

        SqlParameter[] parameters =
        [
            new("Id", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Id)).Type) { Value = id },
            new("Name", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Name)).Type) { Value = name },
            new("Version", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Version)).Type) { Value = version },
            new("Date", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.CreationDate)).Type) { Value = creationDate },
            new("Definition", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.Definition)).Type) { Value = definition },
            new("TenantId", DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.TenantId)).Type) { Value = tenantId ?? (object)DBNull.Value }
        ];

        WorkflowFormEntity[] forms = transaction is null
            ? await SelectAsync(connection, commandText, parameters).ConfigureAwait(false)
            : await SelectAsync(connection, commandText, transaction, parameters).ConfigureAwait(false);
        return forms.FirstOrDefault();
    }

    private string GetTenantFilter(string parameterName, string tenantId)
    {
        return tenantId is null
            ? "[TenantId] IS NULL"
            : $"[TenantId] = @{parameterName}";
    }

    private string GetAccessibleTenantFilter(string parameterName, string tenantId)
    {
        return tenantId is null
            ? "[TenantId] IS NULL"
            : $"([TenantId] = @{parameterName} OR [TenantId] IS NULL)";
    }

    private string GetVersionScopeFilter(string tenantParameterName, string nameParameterName, string tenantId)
    {
        return tenantId is null
            ? "[TenantId] IS NULL"
            : $"""
               (
                   [TenantId] = @{tenantParameterName}
                   OR (
                       [TenantId] IS NULL
                       AND NOT EXISTS (
                           SELECT 1
                           FROM {ObjectName} TenantScope
                           WHERE TenantScope.[Name] = @{nameParameterName}
                             AND TenantScope.[TenantId] = @{tenantParameterName}
                       )
                   )
               )
               """;
    }

    private string GetPreferredScopeOrder(string parameterName, string tenantId, bool includeVersionOrder)
    {
        return tenantId is null
            ? "[Version] DESC"
            : includeVersionOrder
                ? $"CASE WHEN [TenantId] = @{parameterName} THEN 0 ELSE 1 END, [Version] DESC"
                : $"CASE WHEN [TenantId] = @{parameterName} THEN 0 ELSE 1 END";
    }

    private void AddTenantParameter(List<SqlParameter> parameters, string parameterName, string tenantId)
    {
        if (tenantId is not null)
        {
            parameters.Add(new SqlParameter(parameterName, DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.TenantId)).Type)
            {
                Value = tenantId
            });
        }
    }

    private SqlParameter[] CreateTenantParameters(string parameterName, string tenantId)
    {
        return tenantId is null
            ? []
            : [new SqlParameter(parameterName, DBColumns.Find(c => c.Name == nameof(WorkflowFormEntity.TenantId)).Type)
                {
                    Value = tenantId
                }];
    }

    private void AddAccessibleTenantParameter(List<SqlParameter> parameters, string parameterName, string tenantId)
    {
        if (tenantId is not null)
        {
            AddTenantParameter(parameters, parameterName, tenantId);
        }
    }

    private SqlParameter[] CreateAccessibleTenantParameters(string parameterName, string tenantId)
    {
        return tenantId is null ? [] : CreateTenantParameters(parameterName, tenantId);
    }
}
