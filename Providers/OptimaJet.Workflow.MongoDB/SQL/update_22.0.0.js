db.WorkflowGlobalParameter.updateMany({ TenantId: { $exists: false } }, { $set: { TenantId: null } });

db.WorkflowScheme.updateMany({ TenantId: { $exists: false } }, { $set: { TenantId: null } });

db.WorkflowProcessScheme.updateMany({ TenantId: { $exists: false } }, { $set: { TenantId: null } });

db.WorkflowForm.updateMany({ TenantId: { $exists: false } }, { $set: { TenantId: null } });

db.WorkflowApprovalHistory.updateMany({ TenantId: { $exists: false } }, { $set: { TenantId: null } });

db.WorkflowInbox.updateMany({ TenantId: { $exists: false } }, { $set: { TenantId: null } });

db.WorkflowProcessTimer.updateMany({ TenantId: { $exists: false } }, { $set: { TenantId: null } });

db.WorkflowProcessTransitionHistory.updateMany({ TenantId: { $exists: false } }, { $set: { TenantId: null } });

db.WorkflowProcessInstance.find({ TenantId: { $type: "string" } }).forEach(function (instance) {
  db.WorkflowApprovalHistory.updateMany({ ProcessId: instance.Id }, { $set: { TenantId: instance.TenantId } });

  db.WorkflowInbox.updateMany({ ProcessId: instance.Id }, { $set: { TenantId: instance.TenantId } });

  db.WorkflowProcessTimer.updateMany({ ProcessId: instance.Id }, { $set: { TenantId: instance.TenantId } });

  db.WorkflowProcessTransitionHistory.updateMany({ ProcessId: instance.Id }, { $set: { TenantId: instance.TenantId } });
});

db.WorkflowGlobalParameter.dropIndex({ Type: 1, Name: 1 });
db.WorkflowGlobalParameter.createIndex({ Type: 1, Name: 1, TenantId: 1 }, { unique: true });

db.WorkflowScheme.dropIndex({ Code: 1 });
db.WorkflowScheme.createIndex({ Code: 1, TenantId: 1 }, { unique: true });

db.WorkflowForm.dropIndex({ Name: 1, Version: 1 });
db.WorkflowForm.createIndex({ Name: 1, Version: 1, TenantId: 1 }, { unique: true });

db.WorkflowProcessScheme.dropIndex({ SchemeCode: 1 });
db.WorkflowProcessScheme.dropIndex({ IsObsolete: 1 });
db.WorkflowProcessScheme.createIndex({ SchemeCode: 1, TenantId: 1, RootSchemeId: 1, IsObsolete: 1 });

const assignmentSystemSchemeCode = "OptimaJet.Workflow.Plugins.AssigmentPlugin.CheckDeadlines";
const assignmentSystemSchemeIds = db.WorkflowProcessScheme
  .find(
    {
      $or: [
        { SchemeCode: assignmentSystemSchemeCode },
        { RootSchemeCode: assignmentSystemSchemeCode }
      ]
    },
    { Id: 1 }
  )
  .toArray()
  .map(function (scheme) {
    return scheme.Id;
  });

const assignmentSystemProcessIds = assignmentSystemSchemeIds.length === 0
  ? []
  : db.WorkflowProcessInstance
    .find({ SchemeId: { $in: assignmentSystemSchemeIds } }, { Id: 1 })
    .toArray()
    .map(function (instance) {
      return instance.Id;
    });

if (assignmentSystemProcessIds.length > 0) {
  db.WorkflowProcessTransitionHistory.deleteMany({ ProcessId: { $in: assignmentSystemProcessIds } });
  db.WorkflowProcessTimer.deleteMany({ ProcessId: { $in: assignmentSystemProcessIds } });
  db.WorkflowInbox.deleteMany({ ProcessId: { $in: assignmentSystemProcessIds } });
  db.WorkflowApprovalHistory.deleteMany({ ProcessId: { $in: assignmentSystemProcessIds } });
  db.WorkflowProcessInstance.deleteMany({ Id: { $in: assignmentSystemProcessIds } });
}

db.WorkflowProcessScheme.deleteMany({
  $or: [
    { SchemeCode: assignmentSystemSchemeCode },
    { RootSchemeCode: assignmentSystemSchemeCode }
  ]
});
