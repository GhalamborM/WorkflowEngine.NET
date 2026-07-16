DELETE FROM [WorkflowProcessInstancePersistence]
WHERE [ProcessId] = 'F02D5A80-A2F4-464F-8098-C349CE2AEA7A';

DELETE FROM [WorkflowProcessTransitionHistory]
WHERE [ProcessId] = 'F02D5A80-A2F4-464F-8098-C349CE2AEA7A';

DELETE FROM [WorkflowProcessTimer]
WHERE [ProcessId] = 'F02D5A80-A2F4-464F-8098-C349CE2AEA7A';

DELETE FROM [WorkflowInbox]
WHERE [ProcessId] = 'F02D5A80-A2F4-464F-8098-C349CE2AEA7A';

DELETE FROM [WorkflowApprovalHistory]
WHERE [ProcessId] = 'F02D5A80-A2F4-464F-8098-C349CE2AEA7A';

DELETE FROM [WorkflowProcessInstanceStatus]
WHERE [Id] = 'F02D5A80-A2F4-464F-8098-C349CE2AEA7A';

DELETE FROM [WorkflowProcessInstance]
WHERE [Id] = 'F02D5A80-A2F4-464F-8098-C349CE2AEA7A';

DELETE FROM [WorkflowProcessScheme]
WHERE [Id] = '7BF7AFA1-AFD5-482D-B090-5A9CE449EB97'
   OR [SchemeCode] = N'OptimaJet.Workflow.Plugins.AssigmentPlugin.CheckDeadlines'
   OR [RootSchemeCode] = N'OptimaJet.Workflow.Plugins.AssigmentPlugin.CheckDeadlines';
