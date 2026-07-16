DELETE FROM `workflowprocessinstancepersistence`
WHERE `ProcessId` = UNHEX('805A2DF0F4A24F468098C349CE2AEA7A');

DELETE FROM `workflowprocesstransitionhistory`
WHERE `ProcessId` = UNHEX('805A2DF0F4A24F468098C349CE2AEA7A');

DELETE FROM `workflowprocesstimer`
WHERE `ProcessId` = UNHEX('805A2DF0F4A24F468098C349CE2AEA7A');

DELETE FROM `workflowinbox`
WHERE `ProcessId` = UNHEX('805A2DF0F4A24F468098C349CE2AEA7A');

DELETE FROM `workflowapprovalhistory`
WHERE `ProcessId` = UNHEX('805A2DF0F4A24F468098C349CE2AEA7A');

DELETE FROM `workflowprocessinstancestatus`
WHERE `Id` = UNHEX('805A2DF0F4A24F468098C349CE2AEA7A');

DELETE FROM `workflowprocessinstance`
WHERE `Id` = UNHEX('805A2DF0F4A24F468098C349CE2AEA7A');

DELETE FROM `workflowprocessscheme`
WHERE `Id` = UNHEX('A1AFF77BD5AF2D48B0905A9CE449EB97')
   OR `SchemeCode` = 'OptimaJet.Workflow.Plugins.AssigmentPlugin.CheckDeadlines'
   OR `RootSchemeCode` = 'OptimaJet.Workflow.Plugins.AssigmentPlugin.CheckDeadlines';
