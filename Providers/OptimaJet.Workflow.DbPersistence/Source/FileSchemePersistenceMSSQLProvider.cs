using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using OptimaJet.Workflow.Core.Persistence;
using OptimaJet.Workflow.Core.Runtime;
#pragma warning disable 1998

namespace OptimaJet.Workflow.DbPersistence
{
    public class FileSchemePersistenceMSSQLProvider : MSSQLProvider
    {
        private readonly string _storePath;
        private SchemeFilePersistence _schemeFilePersistence;

        public FileSchemePersistenceMSSQLProvider(string storePath, string connectionString, string schemaName = "dbo", 
            bool writeToHistory = true, bool writeSubProcessToRoot = true)
            : base(connectionString, schemaName, writeToHistory, writeSubProcessToRoot)
        {
            _storePath = storePath;
        }

        public override async Task AddSchemeTagsAsync(string schemeCode, IEnumerable<string> tags)
        {
            _schemeFilePersistence.AddSchemeTags(schemeCode, tags);
        }

        public override async Task AddSchemeTagsAsync(string schemeCode, string tenantId, IEnumerable<string> tags)
        {
            _schemeFilePersistence.AddSchemeTags(schemeCode, tags);
        }

        public override async Task<List<string>> GetInlinedSchemeCodesAsync(string tenantId = null)
        {
            return _schemeFilePersistence.GetInlinedSchemeCodes();
        }

        public override async Task<List<string>> GetRelatedByInliningSchemeCodesAsync(string schemeCode, string tenantId = null)
        {
            return _schemeFilePersistence.GetRelatedByInliningSchemeCodes(schemeCode);
        }

        public override async Task<XElement> GetSchemeAsync(string code, string tenantId = null)
        {
            return _schemeFilePersistence.GetScheme(code);
        }

        public override async Task RemoveSchemeTagsAsync(string schemeCode, IEnumerable<string> tags)
        {
            _schemeFilePersistence.RemoveSchemeTags(schemeCode, tags);
        }

        public override async Task RemoveSchemeTagsAsync(string schemeCode, string tenantId, IEnumerable<string> tags)
        {
            _schemeFilePersistence.RemoveSchemeTags(schemeCode, tags);
        }

        public override async Task SaveSchemeAsync(string schemaCode, bool canBeInlined, List<string> inlinedSchemes, string scheme, List<string> tags, string tenantId = null)
        {
            _schemeFilePersistence.SaveScheme(schemaCode, canBeInlined, inlinedSchemes, scheme, tags);
        }

        public override async Task<List<string>> SearchSchemesByTagsAsync(IEnumerable<string> tags)
        {
            return _schemeFilePersistence.SearchSchemesByTags(tags);
        }

        public override async Task<List<string>> SearchSchemesByTagsAsync(string tenantId, IEnumerable<string> tags)
        {
            return _schemeFilePersistence.SearchSchemesByTags(tags);
        }

        public override async Task SetSchemeTagsAsync(string schemeCode, IEnumerable<string> tags)
        {
            _schemeFilePersistence.SetSchemeTags(schemeCode, tags);
        }

        public override async Task SetSchemeTagsAsync(string schemeCode, string tenantId, IEnumerable<string> tags)
        {
            _schemeFilePersistence.SetSchemeTags(schemeCode, tags);
        }

        public override async Task<List<string>> SearchSchemesByTagsInTenantAsync(string tenantId, params string[] tags)
        {
            return _schemeFilePersistence.SearchSchemesByTags(tags);
        }

        public override async Task AddSchemeTagsInTenantAsync(string schemeCode, string tenantId, params string[] tags)
        {
            _schemeFilePersistence.AddSchemeTags(schemeCode, tags);
        }

        public override async Task RemoveSchemeTagsInTenantAsync(string schemeCode, string tenantId, params string[] tags)
        {
            _schemeFilePersistence.RemoveSchemeTags(schemeCode, tags);
        }

        public override async Task SetSchemeTagsInTenantAsync(string schemeCode, string tenantId, params string[] tags)
        {
            _schemeFilePersistence.SetSchemeTags(schemeCode, tags);
        }

        public override void Init(WorkflowRuntime runtime)
        {
            base.Init(runtime);
            _schemeFilePersistence = new SchemeFilePersistence(_storePath, runtime);
        }
    }
}
