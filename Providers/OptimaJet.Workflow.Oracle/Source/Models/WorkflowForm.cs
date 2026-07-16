using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using OptimaJet.Workflow.Core.Entities;
using OptimaJet.Workflow.Oracle;

namespace OptimaJet.Workflow.Oracle.Models;

public class WorkflowForm : DbObject<WorkflowFormEntity>
{
    private const int CreateNewFormVersionMaxAttempts = 3;
    private const int CreateNewFormVersionRetryDelayMilliseconds = 50;

    public WorkflowForm(string schemaName, int commandTimeout) : base(schemaName, nameof(WorkflowForm), commandTimeout)
    {
        DBColumns.AddRange([
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Id), IsKey = true, Type = OracleDbType.Raw },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Name) },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Version), Type = OracleDbType.Int32 },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.CreationDate), Type = OracleDbType.TimeStamp },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.UpdatedDate), Type = OracleDbType.TimeStamp },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.Definition) },
            new ColumnInfo { Name = "LOCKFLAG", Type = OracleDbType.Int32 },
            new ColumnInfo { Name = nameof(WorkflowFormEntity.TenantId), Type = OracleDbType.NVarchar2 }
        ]);
    }

    public async Task<List<string>> GetFormNamesAsync(OracleConnection connection, string tenantId = null)
    {
        string commandText = $"""
                              SELECT DISTINCT {nameof(WorkflowFormEntity.Name)}
                              FROM {ObjectName}
                              WHERE {GetAccessibleTenantFilter("tenantId", tenantId)}
                              """;

        WorkflowFormEntity[] formNames = await SelectAsync(connection, commandText,
                CreateAccessibleTenantParameters("tenantId", tenantId))
            .ConfigureAwait(false);
        return formNames.Select(f => f.Name).ToList();
    }

    public async Task<WorkflowFormEntity> GetFormAsync(OracleConnection connection, string name, int? version = null,
        string tenantId = null)
    {
        return await GetPreferredFormAsync(connection, transaction: null, name, version, tenantId).ConfigureAwait(false);
    }

    public async Task<List<int>> GetFormVersionsAsync(OracleConnection connection, string name, string tenantId = null)
    {
        string commandText = $"""
                              SELECT DISTINCT {nameof(WorkflowFormEntity.Version)}
                              FROM {ObjectName}
                              WHERE {nameof(WorkflowFormEntity.Name)} = :name
                                AND {GetVersionScopeFilter("tenantId", "name", tenantId)}
                              """;

        var parameters = new List<OracleParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name)
        };

        AddAccessibleTenantParameter(parameters, "tenantId", tenantId);

        WorkflowFormEntity[] forms = await SelectAsync(connection, commandText, parameters.ToArray()).ConfigureAwait(false);
        return forms.Select(f => f.Version).ToList();
    }

    public async Task<WorkflowFormEntity> CreateNewFormVersionAsync(OracleConnection connection, DateTime creationDate, string name,
        string defaultDefinition, int? version = null, string tenantId = null)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }

        for (int attempt = 0; attempt < CreateNewFormVersionMaxAttempts; attempt++)
        {
            await using OracleTransaction transaction = connection.BeginTransaction();
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
            catch (OracleException ex) when (ex.IsDuplicateKeyException())
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

    public async Task<WorkflowFormEntity> CreateNewFormIfNotExistsAsync(OracleConnection connection, DateTime creationDate, string name,
        string defaultDefinition, string tenantId = null)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync().ConfigureAwait(false);
        }

        await using OracleTransaction transaction = connection.BeginTransaction();
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
        catch (OracleException ex) when (ex.IsDuplicateKeyException())
        {
            transaction.Rollback();
        }

        WorkflowFormEntity form =
            await GetExactScopeFormAsync(connection, transaction: null, name, version: null, tenantId)
                .ConfigureAwait(false);
        return form ?? throw new Exception("Unable to create a new form.");
    }

    public async Task<int> UpdateFormAsync(OracleConnection connection, string name, int version, long oldLock, long newLock,
        string definition, DateTime updatedDate, string tenantId = null)
    {
        string command = $"""
                          UPDATE {ObjectName} SET
                              {nameof(WorkflowFormEntity.Definition)} = :pDefText,
                              LOCKFLAG = :pNewLock,
                              {nameof(WorkflowFormEntity.UpdatedDate)} = :pDate
                          WHERE {nameof(WorkflowFormEntity.Name)} = :pName
                            AND {nameof(WorkflowFormEntity.Version)} = :pVersion
                            AND LOCKFLAG = :pOldLock
                            AND {GetTenantFilter("tenantId", tenantId)}
                          """;

        var parameters = new List<OracleParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Definition), "pDefText", definition),
            CreateParameter("LOCKFLAG", "pNewLock", newLock),
            CreateParameter("LOCKFLAG", "pOldLock", oldLock),
            CreateParameter(nameof(WorkflowFormEntity.Name), "pName", name),
            CreateParameter(nameof(WorkflowFormEntity.Version), "pVersion", version),
            CreateParameter(nameof(WorkflowFormEntity.UpdatedDate), "pDate", updatedDate)
        };

        AddTenantParameter(parameters, "tenantId", tenantId);

        return await ExecuteCommandNonQueryAsync(connection, command, parameters.ToArray()).ConfigureAwait(false);
    }

    public async Task DeleteFormVersionAsync(OracleConnection connection, string name, int version, string tenantId = null)
    {
        string command = $"""
                          DELETE FROM {ObjectName}
                          WHERE {nameof(WorkflowFormEntity.Name)} = :name
                            AND {nameof(WorkflowFormEntity.Version)} = :version
                            AND {GetTenantFilter("tenantId", tenantId)}
                          """;

        var parameters = new List<OracleParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name),
            CreateParameter(nameof(WorkflowFormEntity.Version), "version", version)
        };

        AddTenantParameter(parameters, "tenantId", tenantId);

        await ExecuteCommandNonQueryAsync(connection, command, parameters.ToArray()).ConfigureAwait(false);
    }

    public async Task DeleteFormAsync(OracleConnection connection, string name, string tenantId = null)
    {
        string command = $"""
                          DELETE FROM {ObjectName}
                          WHERE {nameof(WorkflowFormEntity.Name)} = :name
                            AND {GetTenantFilter("tenantId", tenantId)}
                          """;

        var parameters = new List<OracleParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name)
        };

        AddTenantParameter(parameters, "tenantId", tenantId);

        await ExecuteCommandNonQueryAsync(connection, command, parameters.ToArray()).ConfigureAwait(false);
    }

    private async Task<WorkflowFormEntity> GetPreferredFormAsync(OracleConnection connection, OracleTransaction transaction, string name,
        int? version, string tenantId)
    {
        string filterParameterName = "tenantIdFilter";
        string orderParameterName = "tenantIdOrder";
        string commandText = version is null
            ? $"""
               SELECT *
               FROM {ObjectName}
               WHERE {nameof(WorkflowFormEntity.Name)} = :name
                 AND {GetAccessibleTenantFilter(filterParameterName, tenantId)}
               ORDER BY {GetPreferredScopeOrder(orderParameterName, tenantId, includeVersionOrder: true)}
               FETCH FIRST 1 ROWS ONLY
               """
            : $"""
               SELECT *
               FROM {ObjectName}
               WHERE {nameof(WorkflowFormEntity.Name)} = :name
                 AND {nameof(WorkflowFormEntity.Version)} = :version
                 AND {GetVersionScopeFilter(filterParameterName, "name", tenantId)}
               ORDER BY {GetPreferredScopeOrder(orderParameterName, tenantId, includeVersionOrder: false)}
               FETCH FIRST 1 ROWS ONLY
               """;

        var parameters = new List<OracleParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name)
        };

        if (version.HasValue)
        {
            parameters.Add(CreateParameter(nameof(WorkflowFormEntity.Version), "version", version.Value));
        }

        AddPreferredTenantParameters(parameters, filterParameterName, orderParameterName, tenantId);

        WorkflowFormEntity[] forms = transaction is null
            ? await SelectAsync(connection, commandText, parameters.ToArray()).ConfigureAwait(false)
            : await SelectAsync(connection, commandText, transaction, parameters.ToArray()).ConfigureAwait(false);

        return forms.FirstOrDefault();
    }

    private Task<WorkflowFormEntity> GetExactScopeLatestFormAsync(OracleConnection connection, OracleTransaction transaction, string name,
        string tenantId)
    {
        return GetExactScopeFormAsync(connection, transaction, name, version: null, tenantId);
    }

    private async Task<WorkflowFormEntity> GetExactScopeFormAsync(OracleConnection connection, OracleTransaction transaction, string name,
        int? version, string tenantId)
    {
        string commandText = version is null
            ? $"""
               SELECT *
               FROM {ObjectName}
               WHERE {nameof(WorkflowFormEntity.Name)} = :name
                 AND {GetTenantFilter("tenantId", tenantId)}
               ORDER BY {nameof(WorkflowFormEntity.Version)} DESC
               FETCH FIRST 1 ROWS ONLY
               """
            : $"""
               SELECT *
               FROM {ObjectName}
               WHERE {nameof(WorkflowFormEntity.Name)} = :name
                 AND {nameof(WorkflowFormEntity.Version)} = :version
                 AND {GetTenantFilter("tenantId", tenantId)}
               """;

        var parameters = new List<OracleParameter>
        {
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name)
        };

        if (version.HasValue)
        {
            parameters.Add(CreateParameter(nameof(WorkflowFormEntity.Version), "version", version.Value));
        }

        AddTenantParameter(parameters, "tenantId", tenantId);

        WorkflowFormEntity[] forms = transaction is null
            ? await SelectAsync(connection, commandText, parameters.ToArray()).ConfigureAwait(false)
            : await SelectAsync(connection, commandText, transaction, parameters.ToArray()).ConfigureAwait(false);

        return forms.FirstOrDefault();
    }

    private async Task InsertFormAsync(OracleConnection connection, OracleTransaction transaction, Guid id, string name, int version,
        DateTime creationDate, string definition, string tenantId)
    {
        string insertQuery = $"""
                              INSERT INTO {ObjectName} (
                                  {nameof(WorkflowFormEntity.Id)}, {nameof(WorkflowFormEntity.Name)}, {nameof(WorkflowFormEntity.Version)},
                                  {nameof(WorkflowFormEntity.CreationDate)}, {nameof(WorkflowFormEntity.UpdatedDate)},
                                  {nameof(WorkflowFormEntity.Definition)}, LOCKFLAG, {nameof(WorkflowFormEntity.TenantId).ToUpperInvariant()}
                              ) VALUES (
                                  :id, :name, :version, :creationDate, :creationDate, :definition, 0, :tenantId
                              )
                              """;

        OracleParameter[] parameters =
        [
            CreateParameter(nameof(WorkflowFormEntity.Id), "id", id.ToByteArray()),
            CreateParameter(nameof(WorkflowFormEntity.Name), "name", name),
            CreateParameter(nameof(WorkflowFormEntity.Version), "version", version),
            CreateParameter(nameof(WorkflowFormEntity.CreationDate), "creationDate", creationDate),
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
        string tenantColumn = nameof(WorkflowFormEntity.TenantId).ToUpperInvariant();
        return tenantId is null
            ? $"{tenantColumn} IS NULL"
            : $"{tenantColumn} = :{parameterName}";
    }

    private string GetAccessibleTenantFilter(string parameterName, string tenantId)
    {
        string tenantColumn = nameof(WorkflowFormEntity.TenantId).ToUpperInvariant();
        return tenantId is null
            ? $"{tenantColumn} IS NULL"
            : $"({tenantColumn} = :{parameterName} OR {tenantColumn} IS NULL)";
    }

    private string GetVersionScopeFilter(string tenantParameterName, string nameParameterName, string tenantId)
    {
        string tenantColumn = nameof(WorkflowFormEntity.TenantId).ToUpperInvariant();
        string nameColumn = nameof(WorkflowFormEntity.Name).ToUpperInvariant();
        return tenantId is null
            ? $"{tenantColumn} IS NULL"
            : $"""
               (
                   {tenantColumn} = :{tenantParameterName}
                   OR (
                       {tenantColumn} IS NULL
                       AND NOT EXISTS (
                           SELECT 1
                           FROM {ObjectName} TenantScope
                           WHERE TenantScope.{nameColumn} = :{nameParameterName}
                             AND TenantScope.{tenantColumn} = :{tenantParameterName}
                       )
                   )
               )
               """;
    }

    private string GetPreferredScopeOrder(string parameterName, string tenantId, bool includeVersionOrder)
    {
        string tenantColumn = nameof(WorkflowFormEntity.TenantId).ToUpperInvariant();
        return tenantId is null
            ? $"{nameof(WorkflowFormEntity.Version)} DESC"
            : includeVersionOrder
                ? $"CASE WHEN {tenantColumn} = :{parameterName} THEN 0 ELSE 1 END, {nameof(WorkflowFormEntity.Version)} DESC"
                : $"CASE WHEN {tenantColumn} = :{parameterName} THEN 0 ELSE 1 END";
    }

    private void AddTenantParameter(List<OracleParameter> parameters, string parameterName, string tenantId)
    {
        if (tenantId is not null)
        {
            parameters.Add(CreateParameter(nameof(WorkflowFormEntity.TenantId), parameterName, tenantId));
        }
    }

    private void AddAccessibleTenantParameter(List<OracleParameter> parameters, string parameterName, string tenantId)
    {
        if (tenantId is not null)
        {
            AddTenantParameter(parameters, parameterName, tenantId);
        }
    }

    private void AddPreferredTenantParameters(List<OracleParameter> parameters, string filterParameterName, string orderParameterName,
        string tenantId)
    {
        if (tenantId is not null)
        {
            parameters.Add(CreateParameter(nameof(WorkflowFormEntity.TenantId), filterParameterName, tenantId));
            parameters.Add(CreateParameter(nameof(WorkflowFormEntity.TenantId), orderParameterName, tenantId));
        }
    }

    private OracleParameter[] CreateTenantParameters(string parameterName, string tenantId)
    {
        return tenantId is null
            ? []
            : [CreateParameter(nameof(WorkflowFormEntity.TenantId), parameterName, tenantId)];
    }

    private OracleParameter[] CreateAccessibleTenantParameters(string parameterName, string tenantId)
    {
        return tenantId is null ? [] : CreateTenantParameters(parameterName, tenantId);
    }

    private OracleParameter CreateParameter(string columnName, string parameterName, object value)
    {
        OracleDbType type = DBColumns.Find(c => c.Name == columnName).Type;
        return new OracleParameter(parameterName, type) { Value = value };
    }
}
