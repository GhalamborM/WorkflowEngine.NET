using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using OptimaJet.Workflow.Core.Entities;

// ReSharper disable once CheckNamespace

namespace OptimaJet.Workflow.DbPersistence
{
    public class WorkflowProcessScheme : DbObject<ProcessSchemeEntity>
    {
        public WorkflowProcessScheme(string schemaName, int commandTimeout) : base(schemaName, "WorkflowProcessScheme", commandTimeout)
        {
            DBColumns.AddRange(new[]
            {
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.Id), IsKey = true, Type = SqlDbType.UniqueIdentifier},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.IsObsolete), Type = SqlDbType.Bit},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.SchemeCode)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.Scheme), Type = SqlDbType.NVarChar, Size = -1},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.RootSchemeId), Type = SqlDbType.UniqueIdentifier},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.RootSchemeCode)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.AllowedActivities)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.StartingTransition)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.TenantId), Size = 128}
            });
        }

        public async Task<ProcessSchemeEntity[]> SelectAsync(SqlConnection connection, string schemeCode,
            string tenantId, bool? isObsolete, Guid? rootSchemeId)
        {
            string selectText = $"SELECT * FROM {ObjectName} " +
                                $"WHERE [{nameof(ProcessSchemeEntity.SchemeCode)}] = @schemecode ";

            var pSchemeCode = new SqlParameter("schemecode", SqlDbType.NVarChar) {Value = schemeCode};
            var parameters = new List<SqlParameter> {pSchemeCode};

            if (isObsolete.HasValue)
            {
                if (isObsolete.Value)
                {
                    selectText += $" AND [{nameof(ProcessSchemeEntity.IsObsolete)}] = 1";
                }
                else
                {
                    selectText += $" AND [{nameof(ProcessSchemeEntity.IsObsolete)}] = 0";
                }
            }

            if (rootSchemeId.HasValue)
            {
                selectText += $" AND [{nameof(ProcessSchemeEntity.RootSchemeId)}] = @drootschemeid";
                var pRootSchemeId = new SqlParameter("drootschemeid", SqlDbType.UniqueIdentifier) {Value = rootSchemeId.Value};
                parameters.Add(pRootSchemeId);
            }
            else
            {
                selectText += $" AND [{nameof(ProcessSchemeEntity.RootSchemeId)}] IS NULL";
            }

            if (tenantId == null)
            {
                selectText += $" AND [{nameof(ProcessSchemeEntity.TenantId)}] IS NULL";
            }
            else
            {
                selectText += $" AND [{nameof(ProcessSchemeEntity.TenantId)}] = @tenantid";
                parameters.Add(new SqlParameter("tenantid", SqlDbType.NVarChar) {Value = tenantId});
            }

            return await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false);
        }

        public async Task<int> SetObsoleteAsync(SqlConnection connection, string schemeCode, string tenantId)
        {
            string command = $"UPDATE {ObjectName} " +
                             $"SET [{nameof(ProcessSchemeEntity.IsObsolete)}] = 1 " +
                             $"WHERE ([{nameof(ProcessSchemeEntity.SchemeCode)}] = @schemecode " +
                             $"OR [{nameof(ProcessSchemeEntity.RootSchemeCode)}] = @schemecode)";

            var parameters = new List<SqlParameter>
            {
                new("schemecode", SqlDbType.NVarChar) {Value = schemeCode}
            };

            if (tenantId != null)
            {
                command += $" AND [{nameof(ProcessSchemeEntity.TenantId)}] = @tenantid";
                parameters.Add(new SqlParameter("tenantid", SqlDbType.NVarChar) {Value = tenantId});
            }

            return await ExecuteCommandNonQueryAsync(connection, command, parameters.ToArray()).ConfigureAwait(false);
        }

        public static async Task DeleteUnusedAsync(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync().ConfigureAwait(false);
            }
            
            using var transaction = connection.BeginTransaction();
            using var cmd = new SqlCommand("dbo.DropUnusedWorkflowProcessScheme", connection)
            {
                CommandType = CommandType.StoredProcedure, Transaction = transaction
            };

            var returnParameter = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);

            if ((int)returnParameter.Value != 0)
            {
                transaction.Rollback();
                throw new Exception("Failed to clean up unused WorkflowProcessSchemes ");
            }

            transaction.Commit();
        }
    }
}
