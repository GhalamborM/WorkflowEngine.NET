using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using OptimaJet.Workflow.Core.Entities;

// ReSharper disable once CheckNamespace
namespace OptimaJet.Workflow.MySQL
{
    public class WorkflowProcessScheme : DbObject<ProcessSchemeEntity>
    {
        public WorkflowProcessScheme(int commandTimeout) : base("workflowprocessscheme", commandTimeout)
        {
            DBColumns.AddRange(new[]
            {
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.Id), IsKey = true, Type = MySqlDbType.Binary},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.IsObsolete), Type = MySqlDbType.Bit},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.SchemeCode)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.Scheme)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.RootSchemeId), Type = MySqlDbType.Binary},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.RootSchemeCode)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.AllowedActivities)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.StartingTransition)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.TenantId), Size = 128}
            });
        }

        public async Task<ProcessSchemeEntity[]> SelectAsync(MySqlConnection connection, string schemeCode, string tenantId,
            bool? isObsolete, Guid? rootSchemeId)
        {
            string selectText = $"SELECT * FROM {DbTableName} " +
                                $"WHERE `{nameof(ProcessSchemeEntity.SchemeCode)}` = @schemecode ";

            var pSchemeCode = new MySqlParameter("schemecode", MySqlDbType.VarString) {Value = schemeCode};
            var parameters = new List<MySqlParameter> {pSchemeCode};

            if (isObsolete.HasValue)
            {
                if (isObsolete.Value)
                {
                    selectText += $" AND `{nameof(ProcessSchemeEntity.IsObsolete)}` = 1";
                }
                else
                {
                    selectText += $" AND `{nameof(ProcessSchemeEntity.IsObsolete)}` = 0";
                }
            }

            if (rootSchemeId.HasValue)
            {
                selectText += $" AND `{nameof(ProcessSchemeEntity.RootSchemeId)}` = @drootschemeid";
                var pRootSchemeId = new MySqlParameter("drootschemeid", MySqlDbType.Binary) {Value = rootSchemeId.Value.ToByteArray()};
                parameters.Add(pRootSchemeId);
            }
            else
            {
                selectText += $" AND `{nameof(ProcessSchemeEntity.RootSchemeId)}` IS NULL";
            }

            if (tenantId == null)
            {
                selectText += $" AND `{nameof(ProcessSchemeEntity.TenantId)}` IS NULL";
            }
            else
            {
                selectText += $" AND `{nameof(ProcessSchemeEntity.TenantId)}` = @tenantid";
                parameters.Add(new MySqlParameter("tenantid", MySqlDbType.VarString) {Value = tenantId});
            }

            return await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false);
        }

        public async Task<int> SetObsoleteAsync(MySqlConnection connection, string schemeCode, string tenantId)
        {
            string command = $"UPDATE {DbTableName} SET `{nameof(ProcessSchemeEntity.IsObsolete)}` = 1 " +
                             $"WHERE (`{nameof(ProcessSchemeEntity.SchemeCode)}` = @schemecode " +
                             $"OR `{nameof(ProcessSchemeEntity.RootSchemeCode)}` = @schemecode)";

            var parameters = new List<MySqlParameter>
            {
                new("schemecode", MySqlDbType.VarString) {Value = schemeCode}
            };

            if (tenantId != null)
            {
                command += $" AND `{nameof(ProcessSchemeEntity.TenantId)}` = @tenantid";
                parameters.Add(new MySqlParameter("tenantid", MySqlDbType.VarString) {Value = tenantId});
            }

            return await ExecuteCommandNonQueryAsync(connection, command, parameters.ToArray()).ConfigureAwait(false);
        }

        public static async Task DeleteUnusedAsync(MySqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync().ConfigureAwait(false);
            }

            using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);
            using var command = new MySqlCommand("SELECT DropUnusedWorkflowProcessScheme()", connection) {Transaction = transaction};

            var status = (int)await command.ExecuteScalarAsync().ConfigureAwait(false);

            if (status != 0)
            {
                transaction.Rollback();
                throw new Exception("Failed to clean up unused WorkflowProcessSchemes ");
            }

            transaction.Commit();
        }
    }
}
