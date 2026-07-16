using FluentMigrator;
using OptimaJet.Workflow.Migrator;

namespace OptimaJet.Workflow.MySQL.Migrations;

[Migration(610)]
[WorkflowEngineMigration("OptimaJet.Workflow.MySQL.Scripts.RemoveAssignmentCheckDeadlinesSystemProcess.sql")]
public class Migration610RemoveAssignmentCheckDeadlinesSystemProcess : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
