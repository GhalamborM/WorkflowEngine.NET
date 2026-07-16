UPDATE `workflowprocessinstancestatus` target
INNER JOIN `workflowprocessinstance` source ON source.`Id` = target.`Id`
SET target.`TenantId` = source.`TenantId`
WHERE source.`TenantId` IS NOT NULL
  AND (target.`TenantId` IS NULL OR target.`TenantId` <> source.`TenantId`);

UPDATE `workflowapprovalhistory` target
INNER JOIN `workflowprocessinstance` source ON source.`Id` = target.`ProcessId`
SET target.`TenantId` = source.`TenantId`
WHERE source.`TenantId` IS NOT NULL
  AND (target.`TenantId` IS NULL OR target.`TenantId` <> source.`TenantId`);

UPDATE `workflowinbox` target
INNER JOIN `workflowprocessinstance` source ON source.`Id` = target.`ProcessId`
SET target.`TenantId` = source.`TenantId`
WHERE source.`TenantId` IS NOT NULL
  AND (target.`TenantId` IS NULL OR target.`TenantId` <> source.`TenantId`);

UPDATE `workflowprocessinstancepersistence` target
INNER JOIN `workflowprocessinstance` source ON source.`Id` = target.`ProcessId`
SET target.`TenantId` = source.`TenantId`
WHERE source.`TenantId` IS NOT NULL
  AND (target.`TenantId` IS NULL OR target.`TenantId` <> source.`TenantId`);

UPDATE `workflowprocesstimer` target
INNER JOIN `workflowprocessinstance` source ON source.`Id` = target.`ProcessId`
SET target.`TenantId` = source.`TenantId`
WHERE source.`TenantId` IS NOT NULL
  AND (target.`TenantId` IS NULL OR target.`TenantId` <> source.`TenantId`);

UPDATE `workflowprocesstransitionhistory` target
INNER JOIN `workflowprocessinstance` source ON source.`Id` = target.`ProcessId`
SET target.`TenantId` = source.`TenantId`
WHERE source.`TenantId` IS NOT NULL
  AND (target.`TenantId` IS NULL OR target.`TenantId` <> source.`TenantId`);
