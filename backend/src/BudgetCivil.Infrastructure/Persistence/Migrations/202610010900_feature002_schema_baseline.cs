using BudgetCivil.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations;

[DbContext(typeof(BudgetCivilDbContext))]
[Migration("202610010900_feature002_schema_baseline")]
public partial class feature002_schema_baseline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var schema in DatabaseSchemas.All)
        {
            migrationBuilder.EnsureSchema(name: schema);
        }

        migrationBuilder.RenameTable(
            name: "permissions",
            schema: DatabaseSchemas.Dbo,
            newName: "permissions",
            newSchema: DatabaseSchemas.Iam);

        migrationBuilder.RenameTable(
            name: "roles",
            schema: DatabaseSchemas.Dbo,
            newName: "roles",
            newSchema: DatabaseSchemas.Iam);

        migrationBuilder.RenameTable(
            name: "role_permissions",
            schema: DatabaseSchemas.Dbo,
            newName: "role_permissions",
            newSchema: DatabaseSchemas.Iam);

        migrationBuilder.RenameTable(
            name: "security_audit_events",
            schema: DatabaseSchemas.Dbo,
            newName: "security_audit_events",
            newSchema: DatabaseSchemas.Audit);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "security_audit_events",
            schema: DatabaseSchemas.Audit,
            newName: "security_audit_events",
            newSchema: DatabaseSchemas.Dbo);

        migrationBuilder.RenameTable(
            name: "role_permissions",
            schema: DatabaseSchemas.Iam,
            newName: "role_permissions",
            newSchema: DatabaseSchemas.Dbo);

        migrationBuilder.RenameTable(
            name: "roles",
            schema: DatabaseSchemas.Iam,
            newName: "roles",
            newSchema: DatabaseSchemas.Dbo);

        migrationBuilder.RenameTable(
            name: "permissions",
            schema: DatabaseSchemas.Iam,
            newName: "permissions",
            newSchema: DatabaseSchemas.Dbo);
    }
}
