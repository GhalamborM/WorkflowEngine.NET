using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using OptimaJet.Workflow.Core.Entities;

// ReSharper disable once CheckNamespace
namespace OptimaJet.Workflow.PostgreSQL
{
    public class WorkflowProcessScheme : DbObject<ProcessSchemeEntity>
    {
        public WorkflowProcessScheme(string schemaName, int commandTimeout) : base(schemaName, "WorkflowProcessScheme", commandTimeout)
        {
            DBColumns.AddRange(new[]
            {
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.Id), IsKey = true, Type = NpgsqlDbType.Uuid},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.IsObsolete), Type = NpgsqlDbType.Boolean},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.SchemeCode)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.Scheme)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.RootSchemeId), Type = NpgsqlDbType.Uuid},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.RootSchemeCode)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.AllowedActivities)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.StartingTransition)},
                new ColumnInfo {Name = nameof(ProcessSchemeEntity.TenantId), Size = 128}
            });
        }

        public async Task<ProcessSchemeEntity[]> SelectAsync(NpgsqlConnection connection, string schemeCode,
            string tenantId, bool? isObsolete, Guid? rootSchemeId)
        {
            string selectText = $"SELECT * FROM {ObjectName} " + 
                                $"WHERE \"{nameof(ProcessSchemeEntity.SchemeCode)}\" = @schemecode";
            var parameters = new List<NpgsqlParameter>
            {
                new("schemecode", NpgsqlDbType.Varchar) {Value = schemeCode}
            };

            if (isObsolete.HasValue)
            {
                if (isObsolete.Value)
                {
                    selectText += $" AND \"{nameof(ProcessSchemeEntity.IsObsolete)}\" = TRUE";
                }
                else
                {
                    selectText += $" AND \"{nameof(ProcessSchemeEntity.IsObsolete)}\" = FALSE";
                }
            }

            if (rootSchemeId.HasValue)
            {
                selectText += $" AND \"{nameof(ProcessSchemeEntity.RootSchemeId)}\" = @rootschemeid";
                parameters.Add(new NpgsqlParameter("rootschemeid", NpgsqlDbType.Uuid) {Value = rootSchemeId.Value});
            }
            else
            {
                selectText += $" AND \"{nameof(ProcessSchemeEntity.RootSchemeId)}\" IS NULL";
            }

            if (tenantId == null)
            {
                selectText += $" AND \"{nameof(ProcessSchemeEntity.TenantId)}\" IS NULL";
            }
            else
            {
                selectText += $" AND \"{nameof(ProcessSchemeEntity.TenantId)}\" = @tenantid";
                parameters.Add(new NpgsqlParameter("tenantid", NpgsqlDbType.Varchar) {Value = tenantId});
            }

            return await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false);
        }

        public async Task<int> SetObsoleteAsync(NpgsqlConnection connection, string schemeCode, string tenantId)
        {
            string command = $"UPDATE {ObjectName} SET " + 
                             $"\"{nameof(ProcessSchemeEntity.IsObsolete)}\" = TRUE WHERE (" + 
                             $"\"{nameof(ProcessSchemeEntity.SchemeCode)}\" = @schemecode " + 
                             $"OR \"{nameof(ProcessSchemeEntity.RootSchemeCode)}\" = @schemecode)";

            var parameters = new List<NpgsqlParameter>
            {
                new("schemecode", NpgsqlDbType.Varchar) {Value = schemeCode}
            };

            if (tenantId != null)
            {
                command += $" AND \"{nameof(ProcessSchemeEntity.TenantId)}\" = @tenantid";
                parameters.Add(new NpgsqlParameter("tenantid", NpgsqlDbType.Varchar) {Value = tenantId});
            }

            return await ExecuteCommandNonQueryAsync(connection, command, parameters.ToArray()).ConfigureAwait(false);
        }
        
        public static async Task DeleteUnusedAsync(NpgsqlConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync().ConfigureAwait(false);
            }
            
            using var transaction = connection.BeginTransaction();
            using var command = new NpgsqlCommand("SELECT \"DropUnusedWorkflowProcessScheme\"()", connection)
            {
                Transaction = transaction
            };
            
            var status = (int) await command.ExecuteScalarAsync().ConfigureAwait(false);
            
            if (status != 0)
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                throw new Exception("Failed to clean up unused WorkflowProcessSchemes ");
            }
            await transaction.CommitAsync().ConfigureAwait(false);
        }
    }
}
