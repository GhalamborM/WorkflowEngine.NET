using FluentMigrator;
using OptimaJet.Workflow.Migrator;

namespace OptimaJet.Workflow.SQLite.Migrations;

[Migration(60)]
[WorkflowEngineMigration("OptimaJet.Workflow.SQLite.Scripts.RemoveAssignmentCheckDeadlinesSystemProcess.sql")]
public class Migration60RemoveAssignmentCheckDeadlinesSystemProcess : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
