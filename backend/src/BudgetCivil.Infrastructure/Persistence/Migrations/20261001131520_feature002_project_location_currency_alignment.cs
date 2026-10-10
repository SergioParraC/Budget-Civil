using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class feature002_project_location_currency_alignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM [param].[countries] WHERE [id] = '85A4F3D6-9C28-4E4D-9A36-CF165E887919')
                    INSERT INTO [param].[countries] ([id], [code], [name], [is_active]) VALUES ('85A4F3D6-9C28-4E4D-9A36-CF165E887919', 'CO', 'Colombia', 1);
                IF NOT EXISTS (SELECT 1 FROM [param].[countries] WHERE [id] = '45D88ED2-6943-4E5C-9C8D-6036F2F90F5E')
                    INSERT INTO [param].[countries] ([id], [code], [name], [is_active]) VALUES ('45D88ED2-6943-4E5C-9C8D-6036F2F90F5E', 'PE', 'Peru', 1);
                IF NOT EXISTS (SELECT 1 FROM [param].[countries] WHERE [id] = '7048B712-7793-4320-A56B-43EB2FD7688D')
                    INSERT INTO [param].[countries] ([id], [code], [name], [is_active]) VALUES ('7048B712-7793-4320-A56B-43EB2FD7688D', 'CL', 'Chile', 1);

                IF NOT EXISTS (SELECT 1 FROM [param].[departments] WHERE [id] = '8FA8B466-F3EA-4491-A2DD-8EB79FBA399E')
                    INSERT INTO [param].[departments] ([id], [country_id], [code], [name], [is_active]) VALUES ('8FA8B466-F3EA-4491-A2DD-8EB79FBA399E', '85A4F3D6-9C28-4E4D-9A36-CF165E887919', 'CUN', 'Cundinamarca', 1);
                IF NOT EXISTS (SELECT 1 FROM [param].[departments] WHERE [id] = 'D2979306-CF4A-42F3-97F7-E4BE359B6E24')
                    INSERT INTO [param].[departments] ([id], [country_id], [code], [name], [is_active]) VALUES ('D2979306-CF4A-42F3-97F7-E4BE359B6E24', '85A4F3D6-9C28-4E4D-9A36-CF165E887919', 'ANT', 'Antioquia', 1);
                IF NOT EXISTS (SELECT 1 FROM [param].[departments] WHERE [id] = '8474CD90-5FA3-4CC6-AB7C-C191BCC22403')
                    INSERT INTO [param].[departments] ([id], [country_id], [code], [name], [is_active]) VALUES ('8474CD90-5FA3-4CC6-AB7C-C191BCC22403', '45D88ED2-6943-4E5C-9C8D-6036F2F90F5E', 'LIM', 'Lima', 1);

                IF NOT EXISTS (SELECT 1 FROM [param].[municipalities] WHERE [id] = '3B23ED9C-DB6A-4D1E-A4D0-CFDD2375C523')
                    INSERT INTO [param].[municipalities] ([id], [department_id], [code], [name], [is_active]) VALUES ('3B23ED9C-DB6A-4D1E-A4D0-CFDD2375C523', '8FA8B466-F3EA-4491-A2DD-8EB79FBA399E', 'BOG', 'Bogota', 1);
                IF NOT EXISTS (SELECT 1 FROM [param].[municipalities] WHERE [id] = 'F7EB7DA9-3D64-4824-AD33-D14E4FDD1175')
                    INSERT INTO [param].[municipalities] ([id], [department_id], [code], [name], [is_active]) VALUES ('F7EB7DA9-3D64-4824-AD33-D14E4FDD1175', '8FA8B466-F3EA-4491-A2DD-8EB79FBA399E', 'SOA', 'Soacha', 1);
                IF NOT EXISTS (SELECT 1 FROM [param].[municipalities] WHERE [id] = '4D915316-6369-443F-ACFC-D9815C68E6CC')
                    INSERT INTO [param].[municipalities] ([id], [department_id], [code], [name], [is_active]) VALUES ('4D915316-6369-443F-ACFC-D9815C68E6CC', 'D2979306-CF4A-42F3-97F7-E4BE359B6E24', 'MED', 'Medellin', 1);
                IF NOT EXISTS (SELECT 1 FROM [param].[municipalities] WHERE [id] = '7B9C1295-3218-4961-96C8-F67ECE88707D')
                    INSERT INTO [param].[municipalities] ([id], [department_id], [code], [name], [is_active]) VALUES ('7B9C1295-3218-4961-96C8-F67ECE88707D', '8474CD90-5FA3-4CC6-AB7C-C191BCC22403', 'LIM', 'Lima', 1);

                IF NOT EXISTS (SELECT 1 FROM [param].[base_currencies] WHERE [id] = 'F28EA3F1-90FD-4C52-8D7D-8B7FEDF51520')
                    INSERT INTO [param].[base_currencies] ([id], [iso_code], [name], [is_active]) VALUES ('F28EA3F1-90FD-4C52-8D7D-8B7FEDF51520', 'COP', 'Peso Colombiano', 1);
                IF NOT EXISTS (SELECT 1 FROM [param].[base_currencies] WHERE [id] = '79A6AFEF-C74D-4126-B039-A0CF6435CAF4')
                    INSERT INTO [param].[base_currencies] ([id], [iso_code], [name], [is_active]) VALUES ('79A6AFEF-C74D-4126-B039-A0CF6435CAF4', 'USD', 'Dolar Estadounidense', 1);
                IF NOT EXISTS (SELECT 1 FROM [param].[base_currencies] WHERE [id] = '94393A3A-FE6E-4C02-AE6D-3EC74666CE16')
                    INSERT INTO [param].[base_currencies] ([id], [iso_code], [name], [is_active]) VALUES ('94393A3A-FE6E-4C02-AE6D-3EC74666CE16', 'EUR', 'Euro', 1);
                """);

            migrationBuilder.AddColumn<decimal>(
                name: "aiu_admin_percent",
                schema: "dbo",
                table: "projects",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "aiu_profit_percent",
                schema: "dbo",
                table: "projects",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "aiu_unforeseen_percent",
                schema: "dbo",
                table: "projects",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "base_currency_id",
                schema: "dbo",
                table: "projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "dbo",
                table: "projects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "country_id",
                schema: "dbo",
                table: "projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "department_id",
                schema: "dbo",
                table: "projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_modified_at_utc",
                schema: "dbo",
                table: "projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_user_id",
                schema: "dbo",
                table: "projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "municipality_id",
                schema: "dbo",
                table: "projects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [dbo].[projects]
                SET
                    [country_id] = ISNULL([country_id], '85A4F3D6-9C28-4E4D-9A36-CF165E887919'),
                    [department_id] = ISNULL([department_id], '8FA8B466-F3EA-4491-A2DD-8EB79FBA399E'),
                    [municipality_id] = ISNULL([municipality_id], '3B23ED9C-DB6A-4D1E-A4D0-CFDD2375C523'),
                    [base_currency_id] = ISNULL([base_currency_id], 'F28EA3F1-90FD-4C52-8D7D-8B7FEDF51520')
                WHERE
                    [country_id] IS NULL OR
                    [department_id] IS NULL OR
                    [municipality_id] IS NULL OR
                    [base_currency_id] IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "base_currency_id",
                schema: "dbo",
                table: "projects",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "country_id",
                schema: "dbo",
                table: "projects",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "department_id",
                schema: "dbo",
                table: "projects",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "municipality_id",
                schema: "dbo",
                table: "projects",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_base_currency_id",
                schema: "dbo",
                table: "projects",
                column: "base_currency_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_country_id",
                schema: "dbo",
                table: "projects",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_department_id",
                schema: "dbo",
                table: "projects",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_last_modified_by_user_id",
                schema: "dbo",
                table: "projects",
                column: "last_modified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_projects_municipality_id",
                schema: "dbo",
                table: "projects",
                column: "municipality_id");

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_projects_base_currencies_base_currency_id",
                schema: "dbo",
                table: "projects",
                column: "base_currency_id",
                principalSchema: "param",
                principalTable: "base_currencies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_projects_countries_country_id",
                schema: "dbo",
                table: "projects",
                column: "country_id",
                principalSchema: "param",
                principalTable: "countries",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_projects_departments_department_id",
                schema: "dbo",
                table: "projects",
                column: "department_id",
                principalSchema: "param",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_projects_municipalities_municipality_id",
                schema: "dbo",
                table: "projects",
                column: "municipality_id",
                principalSchema: "param",
                principalTable: "municipalities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_projects_user_accounts_last_modified_by_user_id",
                schema: "dbo",
                table: "projects",
                column: "last_modified_by_user_id",
                principalSchema: "dbo",
                principalTable: "user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dbo_projects_base_currencies_base_currency_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_projects_countries_country_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_projects_departments_department_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_projects_municipalities_municipality_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_projects_user_accounts_last_modified_by_user_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "IX_dbo_projects_base_currency_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "IX_dbo_projects_country_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "IX_dbo_projects_department_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "IX_dbo_projects_last_modified_by_user_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "IX_dbo_projects_municipality_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "aiu_admin_percent",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "aiu_profit_percent",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "aiu_unforeseen_percent",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "base_currency_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "country_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "department_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "last_modified_at_utc",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "last_modified_by_user_id",
                schema: "dbo",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "municipality_id",
                schema: "dbo",
                table: "projects");
        }
    }
}
