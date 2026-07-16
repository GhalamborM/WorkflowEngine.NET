using FluentMigrator;
using OptimaJet.Workflow.Migrator;

namespace OptimaJet.Workflow.MySQL.Migrations;

[Migration(620)]
[WorkflowEngineMigration("OptimaJet.Workflow.MySQL.Scripts.DropEmptyWorkflowProcessAssignmentTable.sql")]
public class Migration620DropEmptyWorkflowProcessAssignmentTable : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
