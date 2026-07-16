using FluentMigrator;
using OptimaJet.Workflow.Migrator;

namespace OptimaJet.Workflow.PostgreSQL.Migrations;

[Migration(70)]
[WorkflowEngineMigration("OptimaJet.Workflow.PostgreSQL.Scripts.DropEmptyWorkflowProcessAssignmentTable.sql")]
public class Migration70DropEmptyWorkflowProcessAssignmentTable : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
