using System.Data;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using OptimaJet.Workflow.Core.Entities;

// ReSharper disable once CheckNamespace
namespace OptimaJet.Workflow.SQLite
{
    public class WorkflowProcessScheme : DbObject<ProcessSchemeEntity>
    {
        public WorkflowProcessScheme(string schemaName, int commandTimeout) : base(schemaName, "WorkflowProcessScheme", commandTimeout)
        {
            DBColumns.AddRange(new[]
            {
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.Id), IsKey = true, Type = DbType.Guid},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.IsObsolete), Type = DbType.Boolean},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.SchemeCode)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.Scheme)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.RootSchemeId), Type = DbType.Guid},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.RootSchemeCode)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.AllowedActivities)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.StartingTransition)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.TenantId), Size = 128}
            });
        }

        public async Task<ProcessSchemeEntity[]> SelectAsync(SqliteConnection connection, string schemeCode, string tenantId,
            bool? isObsolete, Guid? rootSchemeId)
        {
            string selectText = $"SELECT * FROM {ObjectName} " + 
                                $"WHERE {nameof(ProcessSchemeEntity.SchemeCode)} = @schemecode ";
            var parameters = new List<SqliteParameter>
            {
                new("schemecode", DbType.String) {Value = schemeCode}
            };

            if (isObsolete.HasValue)
            {
                if (isObsolete.Value)
                {
                    selectText += $" AND {nameof(ProcessSchemeEntity.IsObsolete)} = TRUE";
                }
                else
                {
                    selectText += $" AND {nameof(ProcessSchemeEntity.IsObsolete)} = FALSE";
                }
            }

            if (rootSchemeId.HasValue)
            {
                selectText += $" AND {nameof(ProcessSchemeEntity.RootSchemeId)} = @rootschemeid";
                parameters.Add(new SqliteParameter("rootschemeid", DbType.String)
                    {Value = ToDbValue(rootSchemeId.Value, DbType.Guid)});
            }
            else
            {
                selectText += $" AND {nameof(ProcessSchemeEntity.RootSchemeId)} IS NULL";
            }

            if (tenantId == null)
            {
                selectText += $" AND {nameof(ProcessSchemeEntity.TenantId)} IS NULL";
            }
            else
            {
                selectText += $" AND {nameof(ProcessSchemeEntity.TenantId)} = @tenantid";
                parameters.Add(new SqliteParameter("tenantid", DbType.String) {Value = tenantId});
            }

            return await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false);
        }

        public async Task<int> SetObsoleteAsync(SqliteConnection connection, string schemeCode, string tenantId)
        {
            string command = $"UPDATE {ObjectName} SET " + 
                             $"{nameof(ProcessSchemeEntity.IsObsolete)} = TRUE WHERE (" + 
                             $"{nameof(ProcessSchemeEntity.SchemeCode)} = @schemecode " + 
                             $"OR {nameof(ProcessSchemeEntity.RootSchemeCode)} = @schemecode)";

            var parameters = new List<SqliteParameter>
            {
                new("schemecode", DbType.String) {Value = schemeCode}
            };

            if (tenantId != null)
            {
                command += $" AND {nameof(ProcessSchemeEntity.TenantId)} = @tenantid";
                parameters.Add(new SqliteParameter("tenantid", DbType.String) {Value = tenantId});
            }

            return await ExecuteCommandNonQueryAsync(connection, command, parameters.ToArray()).ConfigureAwait(false);
        }

        public async Task DeleteUnusedAsync(SqliteConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync().ConfigureAwait(false);
            }
            
            using var transaction = connection.BeginTransaction();
            const string deleteText = "DELETE FROM WorkflowProcessScheme AS wps " +
                                      "WHERE wps.IsObsolete = 1 AND NOT EXISTS " +
                                      "(SELECT * FROM WorkflowProcessInstance AS wpi WHERE wpi.SchemeId = wps.Id)";
            
            await ExecuteCommandNonQueryAsync(connection, deleteText, transaction).ConfigureAwait(false);

            const string selectText = "SELECT COUNT(*) " +
                                      "FROM WorkflowProcessInstance AS wpi " + 
                                      "LEFT OUTER JOIN WorkflowProcessScheme AS wps ON wpi.SchemeId = wps.Id " +
                                      "WHERE wps.Id IS NULL";

            var result = await ExecuteCommandScalarAsync(connection, selectText, transaction).ConfigureAwait(false);
            result = (result == DBNull.Value) ? null : result;
            int rowcount = Convert.ToInt32(result);
            
            if (rowcount != 0)
            {
                transaction.Rollback();
                throw new Exception("Failed to clean up unused WorkflowProcessSchemes");
            }
            
            transaction.Commit();
        }
    }
}
