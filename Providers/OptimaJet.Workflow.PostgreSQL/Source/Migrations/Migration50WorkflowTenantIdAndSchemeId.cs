using FluentMigrator;
using OptimaJet.Workflow.Migrator;

namespace OptimaJet.Workflow.PostgreSQL.Migrations;

[Migration(50)]
[WorkflowEngineMigration("OptimaJet.Workflow.PostgreSQL.Scripts.WorkflowTenantIdAndSchemeId.sql")]
public class Migration50WorkflowTenantIdAndSchemeId : Migration
{
    public override void Up()
    {
        this.EmbeddedScript();
    }

    public override void Down()
    {
    }
}
