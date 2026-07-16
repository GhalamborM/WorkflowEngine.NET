CREATE INDEX `ix_wfps_schemecode_tenantid_rootschemeid_isobsolete`
    ON `workflowprocessscheme` (`SchemeCode`, `TenantId`, `RootSchemeId`, `IsObsolete`);
