using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class resources_add_e3a : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "resource_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_resource_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    resource_type_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    municipality_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    unit_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    distributor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    user_key = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contact_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    quotation_status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    origin_resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_modified_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    deleted_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_resources", x => x.id);
                    table.CheckConstraint("CK_resources_quotation_status", "quotation_status IN ('FALTA_INFORMACION','POR_COTIZAR','COTIZADO')");
                    table.CheckConstraint("CK_resources_unit_price", "unit_price IS NULL OR unit_price >= 0");
                    table.ForeignKey(
                        name: "FK_dbo_resources_municipalities_municipality_id",
                        column: x => x.municipality_id,
                        principalTable: "municipalities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_resources_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_resources_resource_types_resource_type_id",
                        column: x => x.resource_type_id,
                        principalTable: "resource_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_resources_resources_origin_resource_id",
                        column: x => x.origin_resource_id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_resources_users_last_modified_by",
                        column: x => x.last_modified_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_resource_types_code",
                table: "resource_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dbo_resources_last_modified_by",
                table: "resources",
                column: "last_modified_by");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_resources_municipality_id",
                table: "resources",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_resources_origin_resource_id",
                table: "resources",
                column: "origin_resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_resources_project_id_description",
                table: "resources",
                columns: new[] { "project_id", "description" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_resources_project_id_quotation_status",
                table: "resources",
                columns: new[] { "project_id", "quotation_status" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_resources_project_id_resource_type_id",
                table: "resources",
                columns: new[] { "project_id", "resource_type_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_resources_resource_type_id",
                table: "resources",
                column: "resource_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resources");

            migrationBuilder.DropTable(
                name: "resource_types");
        }
    }
}
