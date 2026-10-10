using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class feature002_pending_reference_tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "param");

            migrationBuilder.CreateTable(
                name: "base_currencies",
                schema: "param",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    iso_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_param_base_currencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "param",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_param_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "document_types",
                schema: "param",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_param_document_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_signatures",
                schema: "iam",
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
                    table.PrimaryKey("PK_iam_user_signatures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                schema: "param",
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
                    table.PrimaryKey("PK_param_departments", x => x.id);
                    table.ForeignKey(
                        name: "FK_param_departments_countries_country_id",
                        column: x => x.country_id,
                        principalSchema: "param",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "municipalities",
                schema: "param",
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
                    table.PrimaryKey("PK_param_municipalities", x => x.id);
                    table.ForeignKey(
                        name: "FK_param_municipalities_departments_department_id",
                        column: x => x.department_id,
                        principalSchema: "param",
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_param_base_currencies_iso_code",
                schema: "param",
                table: "base_currencies",
                column: "iso_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_param_countries_code",
                schema: "param",
                table: "countries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_param_departments_country_id_code",
                schema: "param",
                table: "departments",
                columns: new[] { "country_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_param_document_types_code",
                schema: "param",
                table: "document_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_param_municipalities_department_id_code",
                schema: "param",
                table: "municipalities",
                columns: new[] { "department_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_iam_user_signatures_user_id",
                schema: "iam",
                table: "user_signatures",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "base_currencies",
                schema: "param");

            migrationBuilder.DropTable(
                name: "document_types",
                schema: "param");

            migrationBuilder.DropTable(
                name: "municipalities",
                schema: "param");

            migrationBuilder.DropTable(
                name: "user_signatures",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "departments",
                schema: "param");

            migrationBuilder.DropTable(
                name: "countries",
                schema: "param");
        }
    }
}
