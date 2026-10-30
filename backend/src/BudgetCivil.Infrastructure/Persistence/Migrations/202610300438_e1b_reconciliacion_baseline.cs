using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class e1b_reconciliacion_baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "base_currencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    iso_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_base_currencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_document_types", x => x.id);
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
                    table.PrimaryKey("PK_dbo_permissions", x => x.id);
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
                    table.PrimaryKey("PK_dbo_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_signatures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    signature_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    signature_image_base64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_user_signatures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_departments", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_departments_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.PrimaryKey("PK_dbo_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "municipalities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    department_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_municipalities", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_municipalities_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    tax_id = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    company_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    territorial_level = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    department_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    municipality_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    representative_mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    representative_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    representative_full_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    representative_document_type_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    representative_document_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_companies", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_companies_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_companies_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_companies_document_types_representative_document_type_id",
                        column: x => x.representative_document_type_id,
                        principalTable: "document_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_companies_municipalities_municipality_id",
                        column: x => x.municipality_id,
                        principalTable: "municipalities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
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
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    first_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    middle_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    second_last_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    document_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    document_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    department_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    municipality_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    professional_card = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    professional_card_issued_at = table.Column<DateOnly>(type: "date", nullable: true),
                    professional_card_file_path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_users_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_dbo_users_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_users_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_users_municipalities_municipality_id",
                        column: x => x.municipality_id,
                        principalTable: "municipalities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_users_roles_role_id",
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
                    table.PrimaryKey("PK_dbo_password_recovery_grants", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_password_recovery_grants_users_enabled_by_user_id",
                        column: x => x.enabled_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_password_recovery_grants_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "password_recovery_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    token_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    consumed_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_password_recovery_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_password_recovery_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    company_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    objective = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    department_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    municipality_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    is_public = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    director_owner_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_modified_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    last_modified_at_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_projects", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_projects_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_projects_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_projects_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_projects_municipalities_municipality_id",
                        column: x => x.municipality_id,
                        principalTable: "municipalities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_projects_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_projects_users_director_owner_user_id",
                        column: x => x.director_owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_projects_users_last_modified_by_user_id",
                        column: x => x.last_modified_by_user_id,
                        principalTable: "users",
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
                    table.PrimaryKey("PK_dbo_security_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_security_audit_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "project_configurations",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    base_currency_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    budget_limit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    aiu_admin_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    aiu_unforeseen_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    aiu_profit_percent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    signature_mode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    signer_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    requires_auditor = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_project_configurations", x => x.project_id);
                    table.ForeignKey(
                        name: "FK_dbo_project_configurations_base_currencies_base_currency_id",
                        column: x => x.base_currency_id,
                        principalTable: "base_currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_project_configurations_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo_project_configurations_users_signer_user_id",
                        column: x => x.signer_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    role_in_project = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    assigned_by_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    assigned_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_project_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_project_members_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_project_members_users_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_project_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_status_transitions",
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
                    table.PrimaryKey("PK_dbo_project_status_transitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_project_status_transitions_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_project_status_transitions_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_visibility_transitions",
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
                    table.PrimaryKey("PK_dbo_project_visibility_transitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_project_visibility_transitions_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_project_visibility_transitions_users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_base_currencies_iso_code",
                table: "base_currencies",
                column: "iso_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_country_id",
                table: "companies",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_created_by_user_id",
                table: "companies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_department_id",
                table: "companies",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_is_active_business_name",
                table: "companies",
                columns: new[] { "is_active", "business_name" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_last_modified_by_user_id",
                table: "companies",
                column: "last_modified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_municipality_id",
                table: "companies",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_representative_document_type_id",
                table: "companies",
                column: "representative_document_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_representative_user_id",
                table: "companies",
                column: "representative_user_id");

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_companies_tax_id",
                table: "companies",
                column: "tax_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_countries_code",
                table: "countries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_departments_country_id_code",
                table: "departments",
                columns: new[] { "country_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_document_types_code",
                table: "document_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_municipalities_department_id_code",
                table: "municipalities",
                columns: new[] { "department_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dbo_password_recovery_grants_enabled_by_user_id",
                table: "password_recovery_grants",
                column: "enabled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_password_recovery_grants_user_id",
                table: "password_recovery_grants",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_password_recovery_tokens_user_id",
                table: "password_recovery_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_password_recovery_tokens_token_hash",
                table: "password_recovery_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_permissions_permission_key",
                table: "permissions",
                column: "permission_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dbo_project_configurations_base_currency_id",
                table: "project_configurations",
                column: "base_currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_project_configurations_signer_user_id",
                table: "project_configurations",
                column: "signer_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_project_members_assigned_by_user_id",
                table: "project_members",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_project_members_user_id",
                table: "project_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_project_members_project_id_user_id",
                table: "project_members",
                columns: new[] { "project_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dbo_project_status_transitions_changed_by_user_id",
                table: "project_status_transitions",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_project_status_transitions_project_id_changed_at_utc",
                table: "project_status_transitions",
                columns: new[] { "project_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_project_visibility_transitions_changed_by_user_id",
                table: "project_visibility_transitions",
                column: "changed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_project_visibility_transitions_project_id_changed_at_utc",
                table: "project_visibility_transitions",
                columns: new[] { "project_id", "changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_company_id_name",
                table: "projects",
                columns: new[] { "company_id", "name" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_company_id_status",
                table: "projects",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_country_id",
                table: "projects",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_created_by_user_id",
                table: "projects",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_department_id",
                table: "projects",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_director_owner_user_id",
                table: "projects",
                column: "director_owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_last_modified_by_user_id",
                table: "projects",
                column: "last_modified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_municipality_id",
                table: "projects",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_status_is_public",
                table: "projects",
                columns: new[] { "status", "is_public" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_role_permissions_role_id_permission_id",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dbo_security_audit_events_actor_user_id",
                table: "security_audit_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_security_audit_events_occurred_at_utc",
                table: "security_audit_events",
                column: "occurred_at_utc");

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_user_signatures_user_id",
                table: "user_signatures",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dbo_users_company_id_role_id_account_enabled",
                table: "users",
                columns: new[] { "company_id", "role_id", "account_enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_users_country_id",
                table: "users",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_users_department_id",
                table: "users",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_users_document_number",
                table: "users",
                column: "document_number");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_users_municipality_id",
                table: "users",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_companies_users_created_by_user_id",
                table: "companies",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_companies_users_last_modified_by_user_id",
                table: "companies",
                column: "last_modified_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_companies_users_representative_user_id",
                table: "companies",
                column: "representative_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_countries_country_id",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_departments_countries_country_id",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_users_countries_country_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_departments_department_id",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_municipalities_departments_department_id",
                table: "municipalities");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_users_departments_department_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_document_types_representative_document_type_id",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_municipalities_municipality_id",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_users_municipalities_municipality_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_users_created_by_user_id",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_users_last_modified_by_user_id",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_users_representative_user_id",
                table: "companies");

            migrationBuilder.DropTable(
                name: "password_recovery_grants");

            migrationBuilder.DropTable(
                name: "password_recovery_tokens");

            migrationBuilder.DropTable(
                name: "project_configurations");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "project_status_transitions");

            migrationBuilder.DropTable(
                name: "project_visibility_transitions");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "security_audit_events");

            migrationBuilder.DropTable(
                name: "user_signatures");

            migrationBuilder.DropTable(
                name: "base_currencies");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "document_types");

            migrationBuilder.DropTable(
                name: "municipalities");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
