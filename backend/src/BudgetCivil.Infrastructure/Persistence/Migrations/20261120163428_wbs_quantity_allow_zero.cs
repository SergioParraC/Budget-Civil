using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class wbs_quantity_allow_zero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_wbs_items_quantity_work",
                table: "wbs_items");

            migrationBuilder.AddCheckConstraint(
                name: "CK_wbs_items_quantity_work",
                table: "wbs_items",
                sql: "quantity_work >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_wbs_items_quantity_work",
                table: "wbs_items");

            migrationBuilder.AddCheckConstraint(
                name: "CK_wbs_items_quantity_work",
                table: "wbs_items",
                sql: "quantity_work > 0");
        }
    }
}
