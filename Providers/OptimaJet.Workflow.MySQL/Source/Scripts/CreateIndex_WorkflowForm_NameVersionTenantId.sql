ALTER TABLE `workflowform`
    ADD COLUMN `TenantIdForUniqueIndex` varchar(128)
        GENERATED ALWAYS AS (IFNULL(`TenantId`, CHAR(0))) STORED,
    ADD UNIQUE INDEX `ix_workflowform_name_version_tenantid` (`Name`, `Version`, `TenantIdForUniqueIndex`);
