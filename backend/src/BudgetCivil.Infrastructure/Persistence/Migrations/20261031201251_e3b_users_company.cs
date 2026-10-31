using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class e3b_users_company : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dbo_users_companies_company_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_dbo_users_company_id_role_id_account_enabled",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_dbo_users_role_id",
                table: "users");

            migrationBuilder.CreateTable(
                name: "users_company",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    company_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    account_enable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_users_company", x => x.id);
                    table.ForeignKey(
                        name: "FK_dbo_users_company_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_users_company_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Backfill: cada usuario con compania vigente conserva su pertenencia en el historial.
            migrationBuilder.Sql(
                """
                INSERT INTO users_company (id, user_id, company_id, account_enable, created_at, updated_at)
                SELECT NEWID(), u.id, u.company_id, 1, u.updated_at_utc, u.updated_at_utc
                FROM users u
                WHERE u.company_id IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_users_role_id_account_enabled",
                table: "users",
                columns: new[] { "role_id", "account_enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_users_company_company_id_account_enable",
                table: "users_company",
                columns: new[] { "company_id", "account_enable" });

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_users_company_user_id",
                table: "users_company",
                column: "user_id",
                unique: true,
                filter: "[account_enable] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dbo_users_role_id_account_enabled",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                table: "users",
                type: "uniqueidentifier",
                nullable: true);

            // Restaura la pertenencia vigente antes de eliminar el historial.
            migrationBuilder.Sql(
                """
                UPDATE u
                SET u.company_id = uc.company_id
                FROM users u
                INNER JOIN users_company uc ON uc.user_id = u.id AND uc.account_enable = 1;
                """);

            migrationBuilder.DropTable(
                name: "users_company");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_users_company_id_role_id_account_enabled",
                table: "users",
                columns: new[] { "company_id", "role_id", "account_enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_users_role_id",
                table: "users",
                column: "role_id");

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_users_companies_company_id",
                table: "users",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
