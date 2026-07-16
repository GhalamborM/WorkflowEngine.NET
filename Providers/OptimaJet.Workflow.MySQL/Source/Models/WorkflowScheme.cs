using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using OptimaJet.Workflow.Core.Builder;
using OptimaJet.Workflow.Core.Entities;
using OptimaJet.Workflow.Core.Fault;
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Persistence;

// ReSharper disable once CheckNamespace
namespace OptimaJet.Workflow.MySQL
{
    public class WorkflowScheme : DbObject<SchemeEntity>
    {
        public WorkflowScheme(int commandTimeout) : base("workflowscheme", commandTimeout)
        {
            DBColumns.AddRange(new[]
            {
                new ColumnInfo {Name = nameof(SchemeEntity.Id), Type = MySqlDbType.Binary},
                new ColumnInfo {Name = nameof(SchemeEntity.Code), IsKey = true},
                new ColumnInfo {Name = nameof(SchemeEntity.Scheme), Type = MySqlDbType.LongText},
                new ColumnInfo {Name = nameof(SchemeEntity.CanBeInlined), Type = MySqlDbType.Bit},
                new ColumnInfo {Name = nameof(SchemeEntity.InlinedSchemes)}, 
                new ColumnInfo {Name = nameof(SchemeEntity.Tags), Type = MySqlDbType.LongText, Size = -1},
                new ColumnInfo {Name = nameof(SchemeEntity.TenantId), Size = 128}
            });
        }

        public async Task<SchemeEntity> SelectByCodeAsync(MySqlConnection connection, string code, string tenantId)
        {
            if (tenantId == null)
            {
                string sharedSelectText = $"SELECT * FROM {DbTableName} " +
                                          $"WHERE `{nameof(SchemeEntity.Code)}` = @code " +
                                          $"AND `{nameof(SchemeEntity.TenantId)}` IS NULL " +
                                          $"LIMIT 1";

                return (await SelectAsync(connection, sharedSelectText,
                        new MySqlParameter("code", MySqlDbType.VarString) {Value = code}).ConfigureAwait(false))
                    .FirstOrDefault();
            }

            string tenantSelectText = $"SELECT * FROM {DbTableName} " +
                                      $"WHERE `{nameof(SchemeEntity.Code)}` = @code " +
                                      $"AND (`{nameof(SchemeEntity.TenantId)}` = @tenantid " +
                                      $"OR `{nameof(SchemeEntity.TenantId)}` IS NULL) " +
                                      $"ORDER BY CASE WHEN `{nameof(SchemeEntity.TenantId)}` = @tenantid THEN 0 ELSE 1 END " +
                                      $"LIMIT 1";

            return (await SelectAsync(connection, tenantSelectText,
                    new MySqlParameter("code", MySqlDbType.VarString) {Value = code},
                    new MySqlParameter("tenantid", MySqlDbType.VarString) {Value = tenantId}).ConfigureAwait(false))
                .FirstOrDefault();
        }

        public async Task<SchemeEntity> SelectByCodeExactAsync(MySqlConnection connection, string code, string tenantId)
        {
            string selectText = $"SELECT * FROM {DbTableName} " +
                                $"WHERE `{nameof(SchemeEntity.Code)}` = @code ";

            var parameters = new List<MySqlParameter>
            {
                new ("code", MySqlDbType.VarString) {Value = code}
            };

            if (tenantId == null)
            {
                selectText += $"AND `{nameof(SchemeEntity.TenantId)}` IS NULL LIMIT 1";
            }
            else
            {
                selectText += $"AND `{nameof(SchemeEntity.TenantId)}` = @tenantid LIMIT 1";
                parameters.Add(new MySqlParameter("tenantid", MySqlDbType.VarString) {Value = tenantId});
            }

            return (await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false))
                .FirstOrDefault();
        }

        public override async Task<int> UpsertAsync(MySqlConnection connection, SchemeEntity entity, MySqlTransaction transaction = null)
        {
            if (transaction != null)
            {
                return await UpsertInternalAsync(connection, entity, transaction).ConfigureAwait(false);
            }

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync().ConfigureAwait(false);
            }

            using var internalTransaction = connection.BeginTransaction();
            int rowcount = await UpsertInternalAsync(connection, entity, internalTransaction).ConfigureAwait(false);
            internalTransaction.Commit();
            return rowcount;
        }

        private async Task<int> UpsertInternalAsync(MySqlConnection connection, SchemeEntity entity,
            MySqlTransaction transaction)
        {
            int rowcount = await UpdateByCodeAndTenantAsync(connection, entity, transaction).ConfigureAwait(false);

            if (rowcount != 0)
            {
                return rowcount;
            }

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            return await InsertAsync(connection, entity, transaction).ConfigureAwait(false);
        }

        private async Task<int> UpdateByCodeAndTenantAsync(MySqlConnection connection, SchemeEntity entity,
            MySqlTransaction transaction)
        {
            string command = $"UPDATE {DbTableName} SET " +
                             $"`{nameof(SchemeEntity.Scheme)}` = @{nameof(SchemeEntity.Scheme)}," +
                             $"`{nameof(SchemeEntity.CanBeInlined)}` = @{nameof(SchemeEntity.CanBeInlined)}," +
                             $"`{nameof(SchemeEntity.InlinedSchemes)}` = @{nameof(SchemeEntity.InlinedSchemes)}," +
                             $"`{nameof(SchemeEntity.Tags)}` = @{nameof(SchemeEntity.Tags)} " +
                             $"WHERE `{nameof(SchemeEntity.Code)}` = @{nameof(SchemeEntity.Code)}";

            var parameters = new List<MySqlParameter>
            {
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.Scheme))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.CanBeInlined))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.InlinedSchemes))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.Tags))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.Code)))
            };

            if (entity.TenantId == null)
            {
                command += $" AND `{nameof(SchemeEntity.TenantId)}` IS NULL";
            }
            else
            {
                command += $" AND `{nameof(SchemeEntity.TenantId)}` = @{nameof(SchemeEntity.TenantId)}";
                parameters.Add(CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.TenantId))));
            }

            return await ExecuteCommandNonQueryAsync(connection, command, transaction, parameters.ToArray())
                .ConfigureAwait(false);
        }

        public async Task<SchemeEntity[]> SelectAllWorkflowSchemesWithPagingAsync(MySqlConnection connection,
            List<(string parameterName, SortDirection sortDirection)> orderParameters, Paging paging)
        {
            return await SelectAllWithPagingAsync(connection, orderParameters, paging).ConfigureAwait(false);
        }
        
        public async Task<List<string>> GetInlinedSchemeCodesAsync(MySqlConnection connection, string tenantId = null)
        {
            string selectText = $"SELECT DISTINCT `{nameof(SchemeEntity.Code)}` FROM {DbTableName} " +
                                $"WHERE `{nameof(SchemeEntity.CanBeInlined)}` = 1";

            var parameters = new List<MySqlParameter>();
            if (tenantId == null)
            {
                selectText += $" AND `{nameof(SchemeEntity.TenantId)}` IS NULL";
            }
            else
            {
                selectText += $" AND (`{nameof(SchemeEntity.TenantId)}` = @tenantid " +
                              $"OR `{nameof(SchemeEntity.TenantId)}` IS NULL)";
                parameters.Add(new MySqlParameter("tenantid", MySqlDbType.VarString) {Value = tenantId});
            }

            return (await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false))
                .Select(sch => sch.Code)
                .Distinct()
                .ToList();
        }
        
        public async Task<List<string>> GetRelatedSchemeCodesAsync(MySqlConnection connection, string schemeCode, string tenantId = null)
        {
            string selectText =  $"SELECT * FROM {DbTableName} " + 
                                 $"WHERE `{nameof(SchemeEntity.InlinedSchemes)}` LIKE CONCAT('%',@search,'%')";

            var parameters = new List<MySqlParameter>
            {
                new ("search", MySqlDbType.VarString) {Value = $"\"{schemeCode}\""}
            };

            if (tenantId != null)
            {
                selectText += $" AND `{nameof(SchemeEntity.TenantId)}` = @tenantId";
                parameters.Add(new MySqlParameter("tenantId", MySqlDbType.VarString) {Value = tenantId});
            }

            return (await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false)).Select(sch=>sch.Code).Distinct().ToList();
        }

        public async Task<List<string>> GetSchemeCodesByTagsAsync(MySqlConnection connection, string tenantId, IEnumerable<string> tags)
        {
            IEnumerable<string> tagsList = tags?.ToList();
            bool isEmpty = tagsList == null || !tagsList.Any();
            
            string query = $"SELECT `{nameof(SchemeEntity.Code)}` FROM {DbTableName} WHERE ";
            var parameters = new List<MySqlParameter>();
            var clauses = new List<string>();

            if (tenantId == null)
            {
                clauses.Add($"`{nameof(SchemeEntity.TenantId)}` IS NULL");
            }
            else
            {
                clauses.Add($"`{nameof(SchemeEntity.TenantId)}` = @tenantid");
                parameters.Add(new MySqlParameter("tenantid", MySqlDbType.VarString) {Value = tenantId});
            }

            if (!isEmpty)
            {
                var likes = new List<string>();
                foreach (string tag in tagsList)
                {
                    string paramName = $"search_{parameters.Count}";
                    string like = $"`{nameof(SchemeEntity.Tags)}` LIKE CONCAT('%',@{paramName},'%')";
                    string paramValue = $"\"{tag}\"";

                    likes.Add(like);
                    parameters.Add(new MySqlParameter(paramName, MySqlDbType.VarString) {Value = paramValue});
                }

                clauses.Add($"({string.Join(" OR ", likes)})");
            }

            query += string.Join(" AND ", clauses);

            return (await SelectAsync(connection, query, parameters.ToArray()).ConfigureAwait(false))
                .Select(sch => sch.Code)
                .Distinct()
                .ToList();
        }

        public async Task AddSchemeTagsAsync(MySqlConnection connection, string schemeCode, string tenantId,
            IEnumerable<string> tags, IWorkflowBuilder builder)
        {
            await UpdateSchemeTagsAsync(connection, schemeCode, tenantId, schemeTags => schemeTags.Concat(tags).ToList(), builder).ConfigureAwait(false);
        }

        public async Task RemoveSchemeTagsAsync(MySqlConnection connection, string schemeCode, string tenantId,
            IEnumerable<string> tags, IWorkflowBuilder builder)
        {
            await UpdateSchemeTagsAsync(connection, schemeCode, tenantId, schemeTags => schemeTags.Where(t => !tags.Contains(t)).ToList(),
                builder).ConfigureAwait(false);
        }

        public async Task SetSchemeTagsAsync(MySqlConnection connection, string schemeCode, string tenantId,
            IEnumerable<string> tags, IWorkflowBuilder builder)
        {
            await UpdateSchemeTagsAsync(connection, schemeCode, tenantId, schemeTags => tags.ToList(), builder).ConfigureAwait(false);
        }

        private async Task UpdateSchemeTagsAsync(MySqlConnection connection, string schemeCode, string tenantId,
            Func<List<string>,List<string>> getNewTags, IWorkflowBuilder builder)
        {
            SchemeEntity scheme = await SelectByCodeExactAsync(connection, schemeCode, tenantId).ConfigureAwait(false);

            if (scheme == null)
            {
                throw SchemeNotFoundException.Create(schemeCode, SchemeLocation.WorkflowScheme);
            }

            List<string> newTags = getNewTags.Invoke(TagHelper.FromTagStringForDatabase(scheme.Tags));
            scheme.Tags = TagHelper.ToTagStringForDatabase(newTags);
            scheme.Scheme = builder.ReplaceTagsInScheme(scheme.Scheme, newTags);

            await UpsertAsync(connection, scheme).ConfigureAwait(false);
        }
    }
}
