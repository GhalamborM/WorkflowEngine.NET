using System.Data;
using Microsoft.Data.Sqlite;
using OptimaJet.Workflow.Core.Builder;
using OptimaJet.Workflow.Core.Entities;
using OptimaJet.Workflow.Core.Fault;
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Persistence;

// ReSharper disable once CheckNamespace
namespace OptimaJet.Workflow.SQLite
{
    public class WorkflowScheme : DbObject<SchemeEntity>
    {
        public WorkflowScheme(string schemaName, int commandTimeout) : base(schemaName, "WorkflowScheme", commandTimeout)
        {
            DBColumns.AddRange(new[]
            {
                new ColumnInfo {Name = nameof(SchemeEntity.Id), Type = DbType.Guid},
                new ColumnInfo {Name = nameof(SchemeEntity.Code), IsKey = true},
                new ColumnInfo {Name = nameof(SchemeEntity.Scheme)},
                new ColumnInfo {Name = nameof(SchemeEntity.CanBeInlined), Type = DbType.Boolean},
                new ColumnInfo {Name = nameof(SchemeEntity.InlinedSchemes)}, 
                new ColumnInfo {Name = nameof(SchemeEntity.Tags)},
                new ColumnInfo {Name = nameof(SchemeEntity.TenantId), Size = 128}
            });
        }

        public async Task<SchemeEntity> SelectByCodeAsync(SqliteConnection connection, string code, string tenantId)
        {
            if (tenantId == null)
            {
                string sharedSelectText = $"SELECT * FROM {ObjectName} " +
                                          $"WHERE {nameof(SchemeEntity.Code)} = @code " +
                                          $"AND {nameof(SchemeEntity.TenantId)} IS NULL " +
                                          $"LIMIT 1";

                return (await SelectAsync(connection, sharedSelectText,
                        new SqliteParameter("code", DbType.String) {Value = code}).ConfigureAwait(false))
                    .FirstOrDefault();
            }

            string tenantSelectText = $"SELECT * FROM {ObjectName} " +
                                      $"WHERE {nameof(SchemeEntity.Code)} = @code " +
                                      $"AND ({nameof(SchemeEntity.TenantId)} = @tenantid " +
                                      $"OR {nameof(SchemeEntity.TenantId)} IS NULL) " +
                                      $"ORDER BY CASE WHEN {nameof(SchemeEntity.TenantId)} = @tenantid THEN 0 ELSE 1 END " +
                                      $"LIMIT 1";

            return (await SelectAsync(connection, tenantSelectText,
                    new SqliteParameter("code", DbType.String) {Value = code},
                    new SqliteParameter("tenantid", DbType.String) {Value = tenantId}).ConfigureAwait(false))
                .FirstOrDefault();
        }

        public async Task<SchemeEntity> SelectByCodeExactAsync(SqliteConnection connection, string code, string tenantId)
        {
            string selectText = $"SELECT * FROM {ObjectName} " +
                                $"WHERE {nameof(SchemeEntity.Code)} = @code ";

            var parameters = new List<SqliteParameter>
            {
                new ("code", DbType.String) {Value = code}
            };

            if (tenantId == null)
            {
                selectText += $"AND {nameof(SchemeEntity.TenantId)} IS NULL LIMIT 1";
            }
            else
            {
                selectText += $"AND {nameof(SchemeEntity.TenantId)} = @tenantid LIMIT 1";
                parameters.Add(new SqliteParameter("tenantid", DbType.String) {Value = tenantId});
            }

            return (await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false))
                .FirstOrDefault();
        }

        public override async Task<int> UpsertAsync(SqliteConnection connection, SchemeEntity entity,
            SqliteTransaction transaction = null)
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

        private async Task<int> UpsertInternalAsync(SqliteConnection connection, SchemeEntity entity,
            SqliteTransaction transaction)
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

        private async Task<int> UpdateByCodeAndTenantAsync(SqliteConnection connection, SchemeEntity entity,
            SqliteTransaction transaction)
        {
            string command = $"UPDATE {ObjectName} SET " +
                             $"{nameof(SchemeEntity.Scheme)} = @{nameof(SchemeEntity.Scheme)}," +
                             $"{nameof(SchemeEntity.CanBeInlined)} = @{nameof(SchemeEntity.CanBeInlined)}," +
                             $"{nameof(SchemeEntity.InlinedSchemes)} = @{nameof(SchemeEntity.InlinedSchemes)}," +
                             $"{nameof(SchemeEntity.Tags)} = @{nameof(SchemeEntity.Tags)} " +
                             $"WHERE {nameof(SchemeEntity.Code)} = @{nameof(SchemeEntity.Code)}";

            var parameters = new List<SqliteParameter>
            {
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.Scheme))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.CanBeInlined))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.InlinedSchemes))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.Tags))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.Code)))
            };

            if (entity.TenantId == null)
            {
                command += $" AND {nameof(SchemeEntity.TenantId)} IS NULL";
            }
            else
            {
                command += $" AND {nameof(SchemeEntity.TenantId)} = @{nameof(SchemeEntity.TenantId)}";
                parameters.Add(CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.TenantId))));
            }

            return await ExecuteCommandNonQueryAsync(connection, command, transaction, parameters.ToArray())
                .ConfigureAwait(false);
        }

        public async Task<SchemeEntity[]> SelectAllWorkflowSchemesWithPagingAsync(SqliteConnection connection,
            List<(string parameterName, SortDirection sortDirection)> orderParameters, Paging paging)
        {
            return await SelectAllWithPagingAsync(connection, orderParameters, paging).ConfigureAwait(false);
        }
        
        public async Task<List<string>> GetInlinedSchemeCodesAsync(SqliteConnection connection, string tenantId = null)
        {
            string selectText = $"SELECT DISTINCT {nameof(SchemeEntity.Code)} FROM {ObjectName} " +
                                $"WHERE {nameof(SchemeEntity.CanBeInlined)} = TRUE";

            var parameters = new List<SqliteParameter>();
            if (tenantId == null)
            {
                selectText += $" AND {nameof(SchemeEntity.TenantId)} IS NULL";
            }
            else
            {
                selectText += $" AND ({nameof(SchemeEntity.TenantId)} = @tenantid " +
                              $"OR {nameof(SchemeEntity.TenantId)} IS NULL)";
                parameters.Add(new SqliteParameter("tenantid", DbType.String) {Value = tenantId});
            }

            return (await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false))
                .Select(sch => sch.Code)
                .Distinct()
                .ToList();
        }
        
        public async Task<List<string>> GetRelatedSchemeCodesAsync(SqliteConnection connection, string schemeCode, string tenantId = null)
        {
            string selectText =  $"SELECT * FROM {ObjectName} " + 
                                 $"WHERE {nameof(SchemeEntity.InlinedSchemes)} LIKE '%' || @search || '%'";

            var parameters = new List<SqliteParameter>
            {
                new ("search", DbType.String) {Value = $"\"{schemeCode}\""}
            };

            if (tenantId != null)
            {
                selectText += $" AND {nameof(SchemeEntity.TenantId)} = @tenantId";
                parameters.Add(new SqliteParameter("tenantId", DbType.String) {Value = tenantId});
            }

            return (await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false)).Select(sch=>sch.Code).Distinct().ToList();
        }

        public async Task<List<string>> GetSchemeCodesByTagsAsync(SqliteConnection connection, string tenantId, IEnumerable<string> tags)
        {
            IEnumerable<string> tagsList = tags?.ToList();
            bool isEmpty = tagsList == null || !tagsList.Any();

            string query = $"SELECT {nameof(SchemeEntity.Code)} FROM {ObjectName} WHERE ";
            var parameters = new List<SqliteParameter>();
            var clauses = new List<string>();

            if (tenantId == null)
            {
                clauses.Add($"{nameof(SchemeEntity.TenantId)} IS NULL");
            }
            else
            {
                clauses.Add($"{nameof(SchemeEntity.TenantId)} = @tenantid");
                parameters.Add(new SqliteParameter("tenantid", DbType.String) {Value = tenantId});
            }

            if (!isEmpty)
            {
                var likes = new List<string>();
                foreach (string tag in tagsList)
                {
                    string paramName = $"search_{parameters.Count}";
                    string like = $"{nameof(SchemeEntity.Tags)} LIKE '%' || @{paramName} || '%'";
                    string paramValue = $"\"{tag}\"";

                    likes.Add(like);
                    parameters.Add(new SqliteParameter(paramName, DbType.String) {Value = paramValue});
                }

                clauses.Add($"({string.Join(" OR ", likes)})");
            }

            query += string.Join(" AND ", clauses);

            return (await SelectAsync(connection, query, parameters.ToArray()).ConfigureAwait(false))
                .Select(sch => sch.Code)
                .Distinct()
                .ToList();
        }

        public async Task AddSchemeTagsAsync(SqliteConnection connection, string schemeCode, string tenantId,
            IEnumerable<string> tags, IWorkflowBuilder builder)
        {
            await UpdateSchemeTagsAsync(connection, schemeCode, tenantId, schemeTags => schemeTags.Concat(tags).ToList(), builder).ConfigureAwait(false);
        }

        public  async Task RemoveSchemeTagsAsync(SqliteConnection connection, string schemeCode,
            string tenantId, IEnumerable<string> tags, IWorkflowBuilder builder)
        {
            await UpdateSchemeTagsAsync(connection, schemeCode, tenantId, schemeTags => schemeTags.Where(t => !tags.Contains(t)).ToList(),
                builder).ConfigureAwait(false);
        }

        public async Task SetSchemeTagsAsync(SqliteConnection connection,
            string schemeCode,
            string tenantId,
            IEnumerable<string> tags,
            IWorkflowBuilder builder)
        {
            await UpdateSchemeTagsAsync(connection, schemeCode, tenantId, schemeTags => tags.ToList(), builder).ConfigureAwait(false);
        }

        private async Task UpdateSchemeTagsAsync(SqliteConnection connection, string schemeCode, string tenantId,
            Func<List<string>, List<string>> getNewTags, IWorkflowBuilder builder)
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
