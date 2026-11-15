using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class apus_remove_yield : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_apus_yield",
                table: "apus");

            migrationBuilder.DropColumn(
                name: "yield",
                table: "apus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "yield",
                table: "apus",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddCheckConstraint(
                name: "CK_apus_yield",
                table: "apus",
                sql: "yield > 0");
        }
    }
}
