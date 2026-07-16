ALTER TABLE `workflowscheme`
    ADD COLUMN `TenantIdForUniqueIndex` varchar(128)
        GENERATED ALWAYS AS (IFNULL(`TenantId`, CHAR(0))) STORED,
    ADD UNIQUE INDEX `ix_workflowscheme_code_tenantid` (`Code`, `TenantIdForUniqueIndex`);
