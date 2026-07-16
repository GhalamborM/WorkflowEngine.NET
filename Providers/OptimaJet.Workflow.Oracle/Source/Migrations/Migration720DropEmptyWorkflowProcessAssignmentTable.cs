using FluentMigrator;
using OptimaJet.Workflow.Migrator;

namespace OptimaJet.Workflow.Oracle.Migrations;

[Migration(720)]
[WorkflowEngineMigration("OptimaJet.Workflow.Oracle.Scripts.DropEmptyWorkflowProcessAssignmentTable.sql")]
public class Migration720DropEmptyWorkflowProcessAssignmentTable : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
