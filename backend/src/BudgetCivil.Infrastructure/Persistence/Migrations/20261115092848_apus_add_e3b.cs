using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class apus_add_e3b : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "quantity_work",
                table: "wbs_items",
                type: "decimal(18,10)",
                precision: 18,
                scale: 10,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<int>(
                name: "quantity_decimals",
                table: "project_configurations",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "rounding_decimals",
                table: "project_configurations",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.CreateTable(
                name: "apus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    unit = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false),
                    yield = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_modified_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_apus", x => x.id);
                    table.CheckConstraint("CK_apus_status", "status IN ('COMPLETO','FALTA_INFORMACION')");
                    table.CheckConstraint("CK_apus_yield", "yield > 0");
                    table.ForeignKey(
                        name: "FK_dbo_apus_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_apus_users_last_modified_by",
                        column: x => x.last_modified_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "matrix_components",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    matrix_resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    component_resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    consumption = table.Column<decimal>(type: "decimal(18,10)", precision: 18, scale: 10, nullable: false),
                    waste_percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_matrix_components", x => x.id);
                    table.CheckConstraint("CK_matrix_components_consumption", "consumption > 0");
                    table.CheckConstraint("CK_matrix_components_waste", "waste_percentage >= 0 AND waste_percentage <= 100");
                    table.ForeignKey(
                        name: "FK_dbo_matrix_components_resources_component_resource_id",
                        column: x => x.component_resource_id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_matrix_components_resources_matrix_resource_id",
                        column: x => x.matrix_resource_id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "apu_resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    apu_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    consumption = table.Column<decimal>(type: "decimal(18,10)", precision: 18, scale: 10, nullable: false),
                    waste_percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_apu_resources", x => x.id);
                    table.CheckConstraint("CK_apu_resources_consumption", "consumption > 0");
                    table.CheckConstraint("CK_apu_resources_waste", "waste_percentage >= 0 AND waste_percentage <= 100");
                    table.ForeignKey(
                        name: "FK_dbo_apu_resources_apus_apu_id",
                        column: x => x.apu_id,
                        principalTable: "apus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_dbo_apu_resources_resources_resource_id",
                        column: x => x.resource_id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_project_configurations_quantity_decimals",
                table: "project_configurations",
                sql: "quantity_decimals >= 0 AND quantity_decimals <= 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_project_configurations_rounding_decimals",
                table: "project_configurations",
                sql: "rounding_decimals >= 0 AND rounding_decimals <= 10");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_apu_resources_apu_id",
                table: "apu_resources",
                column: "apu_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_apu_resources_resource_id",
                table: "apu_resources",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_apus_last_modified_by",
                table: "apus",
                column: "last_modified_by");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_apus_project_id_status",
                table: "apus",
                columns: new[] { "project_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_matrix_components_component_resource_id",
                table: "matrix_components",
                column: "component_resource_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_matrix_components_matrix_resource_id",
                table: "matrix_components",
                column: "matrix_resource_id");

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_wbs_items_apus_apu_id",
                table: "wbs_items",
                column: "apu_id",
                principalTable: "apus",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dbo_wbs_items_apus_apu_id",
                table: "wbs_items");

            migrationBuilder.DropTable(
                name: "apu_resources");

            migrationBuilder.DropTable(
                name: "matrix_components");

            migrationBuilder.DropTable(
                name: "apus");

            migrationBuilder.DropCheckConstraint(
                name: "CK_project_configurations_quantity_decimals",
                table: "project_configurations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_project_configurations_rounding_decimals",
                table: "project_configurations");

            migrationBuilder.DropColumn(
                name: "quantity_decimals",
                table: "project_configurations");

            migrationBuilder.DropColumn(
                name: "rounding_decimals",
                table: "project_configurations");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity_work",
                table: "wbs_items",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,10)",
                oldPrecision: 18,
                oldScale: 10);
        }
    }
}
