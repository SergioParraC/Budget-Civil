using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class e3a_suppliers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "supplier_id",
                table: "resources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    company_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    business_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    contact_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    contact_phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    observations = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    country_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    department_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    municipality_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_modified_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_suppliers", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_suppliers_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_suppliers_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_suppliers_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_suppliers_municipalities_municipality_id",
                        column: x => x.municipality_id,
                        principalTable: "municipalities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_suppliers_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_suppliers_users_last_modified_by",
                        column: x => x.last_modified_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_resources_supplier_id",
                table: "resources",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_suppliers_company_id_business_name",
                table: "suppliers",
                columns: new[] { "company_id", "business_name" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_suppliers_company_id_country_id",
                table: "suppliers",
                columns: new[] { "company_id", "country_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_suppliers_company_id_department_id",
                table: "suppliers",
                columns: new[] { "company_id", "department_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_suppliers_company_id_is_active",
                table: "suppliers",
                columns: new[] { "company_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_suppliers_company_id_municipality_id",
                table: "suppliers",
                columns: new[] { "company_id", "municipality_id" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_suppliers_country_id",
                table: "suppliers",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_suppliers_created_by",
                table: "suppliers",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_suppliers_department_id",
                table: "suppliers",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_suppliers_last_modified_by",
                table: "suppliers",
                column: "last_modified_by");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_suppliers_municipality_id",
                table: "suppliers",
                column: "municipality_id");

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_resources_suppliers_supplier_id",
                table: "resources",
                column: "supplier_id",
                principalTable: "suppliers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dbo_resources_suppliers_supplier_id",
                table: "resources");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropIndex(
                name: "IX_dbo_resources_supplier_id",
                table: "resources");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "resources");
        }
    }
}
