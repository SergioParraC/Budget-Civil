using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class units_add_catalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "unit",
                table: "apus",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(5)",
                oldUnicode: false,
                oldMaxLength: 5);

            migrationBuilder.CreateTable(
                name: "measurement_units",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    short_unit = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    unit = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_measurement_units", x => x.id);
                    table.CheckConstraint("CK_measurement_units_short_unit_not_empty", "LEN(LTRIM(RTRIM(short_unit))) > 0");
                    table.CheckConstraint("CK_measurement_units_unit_not_empty", "LEN(LTRIM(RTRIM(unit))) > 0");
                });

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_measurement_units_short_unit",
                table: "measurement_units",
                column: "short_unit",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "measurement_units");

            migrationBuilder.AlterColumn<string>(
                name: "unit",
                table: "apus",
                type: "varchar(5)",
                unicode: false,
                maxLength: 5,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldUnicode: false,
                oldMaxLength: 10);
        }
    }
}
