using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class feature002_schema_alignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "projects",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    company_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    objective = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_public = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    signing_mode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    signer_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    director_owner_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_projects", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_projects_companies_company_id",
                        column: x => x.company_id,
                        principalSchema: "dbo",
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_projects_user_accounts_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "dbo",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_projects_user_accounts_director_owner_user_id",
                        column: x => x.director_owner_user_id,
                        principalSchema: "dbo",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_projects_user_accounts_signer_user_id",
                        column: x => x.signer_user_id,
                        principalSchema: "dbo",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "project_status_transitions",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    previous_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    new_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    transition_kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_project_status_transitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_project_status_transitions_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "dbo",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_project_status_transitions_user_accounts_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalSchema: "dbo",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_user_memberships",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    assigned_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ended_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_project_user_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_project_user_memberships_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "dbo",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_project_user_memberships_user_accounts_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalSchema: "dbo",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_project_user_memberships_user_accounts_user_id",
                        column: x => x.user_id,
                        principalSchema: "dbo",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_visibility_transitions",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    previous_is_public = table.Column<bool>(type: "bit", nullable: false),
                    new_is_public = table.Column<bool>(type: "bit", nullable: false),
                    changed_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    changed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_project_visibility_transitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_project_visibility_transitions_projects_project_id",
                        column: x => x.project_id,
                        principalSchema: "dbo",
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_audit_project_visibility_transitions_user_accounts_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalSchema: "dbo",
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_user_accounts_company_id_role_id_account_enabled",
                schema: "dbo",
                table: "user_accounts",
                columns: new[] { "company_id", "role_id", "account_enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_is_active_business_name",
                schema: "dbo",
                table: "companies",
                columns: new[] { "is_active", "business_name" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_project_status_transitions_changed_by_user_id",
                schema: "audit",
                table: "project_status_transitions",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_project_status_transitions_project_id_changed_at_utc",
                schema: "audit",
                table: "project_status_transitions",
                columns: new[] { "project_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_project_user_memberships_assigned_by_user_id",
                schema: "dbo",
                table: "project_user_memberships",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_project_user_memberships_user_id",
                schema: "dbo",
                table: "project_user_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_project_user_memberships_project_id_user_id",
                schema: "dbo",
                table: "project_user_memberships",
                columns: new[] { "project_id", "user_id" },
                unique: true,
                filter: "[is_active] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_audit_project_visibility_transitions_changed_by_user_id",
                schema: "audit",
                table: "project_visibility_transitions",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_project_visibility_transitions_project_id_changed_at_utc",
                schema: "audit",
                table: "project_visibility_transitions",
                columns: new[] { "project_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_company_id_name",
                schema: "dbo",
                table: "projects",
                columns: new[] { "company_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_company_id_status",
                schema: "dbo",
                table: "projects",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_created_by_user_id",
                schema: "dbo",
                table: "projects",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_director_owner_user_id",
                schema: "dbo",
                table: "projects",
                column: "director_owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_signer_user_id",
                schema: "dbo",
                table: "projects",
                column: "signer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_status_is_public",
                schema: "dbo",
                table: "projects",
                columns: new[] { "status", "is_public" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_status_transitions",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "project_user_memberships",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "project_visibility_transitions",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "projects",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "IX_dbo_user_accounts_company_id_role_id_account_enabled",
                schema: "dbo",
                table: "user_accounts");

            migrationBuilder.DropIndex(
                name: "IX_dbo_companies_is_active_business_name",
                schema: "dbo",
                table: "companies");
        }
    }
}
