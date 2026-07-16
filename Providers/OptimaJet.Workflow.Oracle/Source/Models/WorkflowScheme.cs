using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using OptimaJet.Workflow.Core.Builder;
using OptimaJet.Workflow.Core.Entities;
using OptimaJet.Workflow.Core.Fault;
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Persistence;
using Oracle.ManagedDataAccess.Client;

// ReSharper disable once CheckNamespace
namespace OptimaJet.Workflow.Oracle
{
    public class WorkflowScheme : DbObject<SchemeEntity>
    {
        public WorkflowScheme(string schemaName, int commandTimeout) : base(schemaName, "WorkflowScheme", commandTimeout)
        {
            DBColumns.AddRange(new[]
            {
                new ColumnInfo {Name = nameof(SchemeEntity.Id), Type = OracleDbType.Raw},
                new ColumnInfo {Name = nameof(SchemeEntity.Code), IsKey = true},
                new ColumnInfo {Name = nameof(SchemeEntity.Scheme), Type = OracleDbType.Clob},
                new ColumnInfo {Name = nameof(SchemeEntity.CanBeInlined), Type = OracleDbType.Byte},
                new ColumnInfo {Name = nameof(SchemeEntity.InlinedSchemes)}, 
                new ColumnInfo {Name = nameof(SchemeEntity.Tags), Type = OracleDbType.NVarchar2},
                new ColumnInfo {Name = nameof(SchemeEntity.TenantId), Size = 128}
            });
        }

        public async Task<SchemeEntity> SelectByCodeAsync(OracleConnection connection, string code, string tenantId)
        {
            if (tenantId == null)
            {
                string sharedSelectText = $"SELECT * FROM {DbTableName} " +
                                          $"WHERE {nameof(SchemeEntity.Code).ToUpperInvariant()} = :code " +
                                          $"AND {nameof(SchemeEntity.TenantId).ToUpperInvariant()} IS NULL " +
                                          $"FETCH NEXT 1 ROWS ONLY";

                return (await SelectAsync(connection, sharedSelectText,
                        new OracleParameter("code", OracleDbType.NVarchar2, code, ParameterDirection.Input))
                    .ConfigureAwait(false)).FirstOrDefault();
            }

            string tenantSelectText = $"SELECT * FROM {DbTableName} " +
                                      $"WHERE {nameof(SchemeEntity.Code).ToUpperInvariant()} = :code " +
                                      $"AND ({nameof(SchemeEntity.TenantId).ToUpperInvariant()} = :tenantid " +
                                      $"OR {nameof(SchemeEntity.TenantId).ToUpperInvariant()} IS NULL) " +
                                      $"ORDER BY CASE WHEN {nameof(SchemeEntity.TenantId).ToUpperInvariant()} = :tenantid THEN 0 ELSE 1 END " +
                                      $"FETCH NEXT 1 ROWS ONLY";

            return (await SelectAsync(connection, tenantSelectText,
                    new OracleParameter("code", OracleDbType.NVarchar2, code, ParameterDirection.Input),
                    new OracleParameter("tenantid", OracleDbType.NVarchar2, tenantId, ParameterDirection.Input))
                .ConfigureAwait(false)).FirstOrDefault();
        }

        public async Task<SchemeEntity> SelectByCodeExactAsync(OracleConnection connection, string code, string tenantId)
        {
            string selectText = $"SELECT * FROM {DbTableName} " +
                                $"WHERE {nameof(SchemeEntity.Code).ToUpperInvariant()} = :code ";

            var parameters = new List<OracleParameter>
            {
                new ("code", OracleDbType.NVarchar2, code, ParameterDirection.Input)
            };

            if (tenantId == null)
            {
                selectText += $"AND {nameof(SchemeEntity.TenantId).ToUpperInvariant()} IS NULL FETCH NEXT 1 ROWS ONLY";
            }
            else
            {
                selectText += $"AND {nameof(SchemeEntity.TenantId).ToUpperInvariant()} = :tenantid FETCH NEXT 1 ROWS ONLY";
                parameters.Add(new OracleParameter("tenantid", OracleDbType.NVarchar2, tenantId, ParameterDirection.Input));
            }

            return (await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false))
                .FirstOrDefault();
        }

        public override async Task<int> UpsertAsync(OracleConnection connection, SchemeEntity entity,
            OracleTransaction transaction = null)
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

        private async Task<int> UpsertInternalAsync(OracleConnection connection, SchemeEntity entity,
            OracleTransaction transaction)
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

        private async Task<int> UpdateByCodeAndTenantAsync(OracleConnection connection, SchemeEntity entity,
            OracleTransaction transaction)
        {
            string command = $"UPDATE {ObjectName} SET " +
                             $"{nameof(SchemeEntity.Scheme).ToUpperInvariant()} = :{nameof(SchemeEntity.Scheme)}," +
                             $"{nameof(SchemeEntity.CanBeInlined).ToUpperInvariant()} = :{nameof(SchemeEntity.CanBeInlined)}," +
                             $"{nameof(SchemeEntity.InlinedSchemes).ToUpperInvariant()} = :{nameof(SchemeEntity.InlinedSchemes)}," +
                             $"{nameof(SchemeEntity.Tags).ToUpperInvariant()} = :{nameof(SchemeEntity.Tags)} " +
                             $"WHERE {nameof(SchemeEntity.Code).ToUpperInvariant()} = :{nameof(SchemeEntity.Code)}";

            var parameters = new List<OracleParameter>
            {
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.Scheme))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.CanBeInlined))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.InlinedSchemes))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.Tags))),
                CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.Code)))
            };

            if (entity.TenantId == null)
            {
                command += $" AND {nameof(SchemeEntity.TenantId).ToUpperInvariant()} IS NULL";
            }
            else
            {
                command += $" AND {nameof(SchemeEntity.TenantId).ToUpperInvariant()} = :{nameof(SchemeEntity.TenantId)}";
                parameters.Add(CreateParameter(entity, DBColumns.Single(c => c.Name == nameof(SchemeEntity.TenantId))));
            }

            return await ExecuteCommandNonQueryAsync(connection, command, transaction, parameters.ToArray())
                .ConfigureAwait(false);
        }

        public async Task<SchemeEntity[]> SelectAllWorkflowSchemesWithPagingAsync(OracleConnection connection,
            List<(string parameterName, SortDirection sortDirection)> orderParameters, Paging paging)
        {
            return await SelectAllWithPagingAsync(connection, orderParameters, paging).ConfigureAwait(false);
        }
        
        public async Task<List<string>> GetInlinedSchemeCodesAsync(OracleConnection connection, string tenantId = null)
        {
            string selectText = $"SELECT DISTINCT {nameof(SchemeEntity.Code).ToUpper()} FROM {DbTableName} " +
                                $"WHERE {nameof(SchemeEntity.CanBeInlined).ToUpper()} = 1";

            var parameters = new List<OracleParameter>();
            if (tenantId == null)
            {
                selectText += $" AND {nameof(SchemeEntity.TenantId).ToUpper()} IS NULL";
            }
            else
            {
                selectText += $" AND ({nameof(SchemeEntity.TenantId).ToUpper()} = :tenantid " +
                              $"OR {nameof(SchemeEntity.TenantId).ToUpper()} IS NULL)";
                parameters.Add(new OracleParameter("tenantid", OracleDbType.NVarchar2, tenantId, ParameterDirection.Input));
            }

            return (await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false))
                .Select(sch => sch.Code)
                .Distinct()
                .ToList();
        }

        public async Task<List<string>> GetRelatedSchemeCodesAsync(OracleConnection connection, string schemeCode, string tenantId = null)
        {
            string selectText = $"SELECT * FROM {DbTableName} " + 
                                $"WHERE {nameof(SchemeEntity.InlinedSchemes).ToUpper()} LIKE '%' || :search || '%'";

            var parameters = new List<OracleParameter>
            {
                new ("search", OracleDbType.NVarchar2, $"\"{schemeCode}\"", ParameterDirection.Input)
            };

            if (tenantId != null)
            {
                selectText += $" AND {nameof(SchemeEntity.TenantId).ToUpper()} = :tenantId";
                parameters.Add(new OracleParameter("tenantId", OracleDbType.NVarchar2, tenantId, ParameterDirection.Input));
            }

            return (await SelectAsync(connection, selectText, parameters.ToArray()).ConfigureAwait(false)).Select(sch => sch.Code).Distinct().ToList();
        }

        public async Task<List<string>> GetSchemeCodesByTagsAsync(OracleConnection connection, string tenantId, IEnumerable<string> tags)
        {
            IEnumerable<string> tagsList = tags?.ToList();

            bool isEmpty = tagsList == null || !tagsList.Any();

            string query = $"SELECT * FROM {DbTableName} WHERE ";
            var parameters = new List<OracleParameter>();
            var clauses = new List<string>();

            if (tenantId == null)
            {
                clauses.Add($"{nameof(SchemeEntity.TenantId).ToUpper()} IS NULL");
            }
            else
            {
                clauses.Add($"{nameof(SchemeEntity.TenantId).ToUpper()} = :tenantid");
                parameters.Add(new OracleParameter("tenantid", OracleDbType.NVarchar2, tenantId, ParameterDirection.Input));
            }

            if (!isEmpty)
            {
                var likes = new List<string>();
                foreach (string tag in tagsList)
                {
                    string paramName = $"search_{parameters.Count}";
                    string like = $"{nameof(SchemeEntity.Tags).ToUpper()} LIKE '%' || :{paramName} || '%'";
                    string paramValue = $"\"{tag}\"";

                    likes.Add(like);
                    parameters.Add(new OracleParameter(paramName, OracleDbType.NVarchar2, paramValue, ParameterDirection.Input));
                }

                clauses.Add($"({string.Join(" OR ", likes)})");
            }

            query += string.Join(" AND ", clauses);

            return (await SelectAsync(connection, query, parameters.ToArray()).ConfigureAwait(false))
                .Select(sch => sch.Code)
                .Distinct()
                .ToList();
        }

        public async Task AddSchemeTagsAsync(OracleConnection connection, string schemeCode, string tenantId,
            IEnumerable<string> tags, IWorkflowBuilder builder)
        {
            await UpdateSchemeTagsAsync(connection, schemeCode, tenantId, schemeTags => schemeTags.Concat(tags).ToList(), builder).ConfigureAwait(false);
        }

        public async Task RemoveSchemeTagsAsync(OracleConnection connection, string schemeCode,
            string tenantId, IEnumerable<string> tags, IWorkflowBuilder builder)
        {
            await UpdateSchemeTagsAsync(connection, schemeCode, tenantId, schemeTags => schemeTags.Where(t => !tags.Contains(t)).ToList(),
                builder).ConfigureAwait(false);
        }

        public async Task SetSchemeTagsAsync(OracleConnection connection,
            string schemeCode,
            string tenantId,
            IEnumerable<string> tags,
            IWorkflowBuilder builder)
        {
            await UpdateSchemeTagsAsync(connection, schemeCode, tenantId, schemeTags => tags.ToList(), builder).ConfigureAwait(false);
        }

        private async Task UpdateSchemeTagsAsync(OracleConnection connection, string schemeCode, string tenantId,
            Func<List<string>, List<string>> getNewTags, IWorkflowBuilder builder)
        {
            var scheme = await SelectByCodeExactAsync(connection, schemeCode, tenantId).ConfigureAwait(false);

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
