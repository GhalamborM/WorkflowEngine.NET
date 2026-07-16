DROP PROCEDURE IF EXISTS `workflow_precheck_tenant_schema`;

CREATE PROCEDURE `workflow_precheck_tenant_schema`()
BEGIN
    IF EXISTS (
        SELECT 1
        FROM `workflowglobalparameter`
        WHERE CHAR_LENGTH(`Type`) > 128
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'BREAKING CHANGES DETECTED: WorkflowGlobalParameter.Type exceeds 128 characters. Contact support@optimajet.com.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM `workflowprocessinstance`
        WHERE CHAR_LENGTH(`TenantId`) > 128
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'BREAKING CHANGES DETECTED: WorkflowProcessInstance.TenantId exceeds 128 characters. Contact support@optimajet.com.';
    END IF;
END;

CALL `workflow_precheck_tenant_schema`();

DROP PROCEDURE `workflow_precheck_tenant_schema`;
