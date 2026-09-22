using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialUsersCompaniesSecurityAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    tax_id = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    permission_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    seed_version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    is_base_role = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    permission_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    assigned_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    seed_version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    company_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    account_enabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    account_locked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    failed_login_attempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    last_login_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_accounts_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_accounts_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "password_recovery_grants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    enabled_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    enabled_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    consumed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_recovery_grants", x => x.id);
                    table.ForeignKey(
                        name: "FK_password_recovery_grants_user_accounts_enabled_by_user_id",
                        column: x => x.enabled_by_user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_password_recovery_grants_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "security_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    company_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    event_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    permission_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    target_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    target_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    reason_code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    metadata_json = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_security_audit_events_user_accounts_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_company_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    company_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    assigned_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ended_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_company_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_company_memberships_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_company_memberships_user_accounts_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_company_memberships_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    second_last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    document_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    document_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    residence_city = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    residence_department = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    residence_country = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    professional_card = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    professional_card_issued_at = table.Column<DateOnly>(type: "date", nullable: true),
                    professional_card_file_path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_user_profiles_user_accounts_user_id",
                        column: x => x.user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_companies_tax_id",
                table: "companies",
                column: "tax_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_recovery_grants_enabled_by_user_id",
                table: "password_recovery_grants",
                column: "enabled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_password_recovery_grants_user_id",
                table: "password_recovery_grants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_permission_key",
                table: "permissions",
                column: "permission_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_role_id_permission_id",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_security_audit_events_actor_user_id",
                table: "security_audit_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_security_audit_events_occurred_at_utc",
                table: "security_audit_events",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_company_id",
                table: "user_accounts",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_email",
                table: "user_accounts",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_role_id",
                table: "user_accounts",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_username",
                table: "user_accounts",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_company_memberships_assigned_by_user_id",
                table: "user_company_memberships",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_company_memberships_company_id",
                table: "user_company_memberships",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_company_memberships_user_id",
                table: "user_company_memberships",
                column: "user_id",
                unique: true,
                filter: "[is_active] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_document_number",
                table: "user_profiles",
                column: "document_number");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_recovery_grants");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "security_audit_events");

            migrationBuilder.DropTable(
                name: "user_company_memberships");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "user_accounts");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
