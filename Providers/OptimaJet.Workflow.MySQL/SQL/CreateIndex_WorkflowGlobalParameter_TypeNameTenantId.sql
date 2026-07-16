ALTER TABLE `workflowglobalparameter`
    ADD COLUMN `TenantIdForUniqueIndex` varchar(128)
        GENERATED ALWAYS AS (IFNULL(`TenantId`, CHAR(0))) STORED,
    ADD UNIQUE INDEX `ix_workflowglobalparameter_type_name_tenantid` (`Type`, `Name`, `TenantIdForUniqueIndex`);
