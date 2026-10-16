using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Feature002GeoProfileCompanyReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "residence_city",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "residence_country",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "residence_department",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                schema: "dbo",
                table: "companies",
                newName: "last_modified_at");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                schema: "dbo",
                table: "companies",
                newName: "created_at");

            migrationBuilder.AddColumn<Guid>(
                name: "country_id",
                schema: "dbo",
                table: "user_profiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "department_id",
                schema: "dbo",
                table: "user_profiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "municipality_id",
                schema: "dbo",
                table: "user_profiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "company_type",
                schema: "dbo",
                table: "companies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "country_id",
                schema: "dbo",
                table: "companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_user_id",
                schema: "dbo",
                table: "companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "department_id",
                schema: "dbo",
                table: "companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_modified_at",
                schema: "dbo",
                table: "companies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by_user_id",
                schema: "dbo",
                table: "companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "municipality_id",
                schema: "dbo",
                table: "companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "representative_document_number",
                schema: "dbo",
                table: "companies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "representative_document_type_id",
                schema: "dbo",
                table: "companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "representative_full_name",
                schema: "dbo",
                table: "companies",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "representative_mode",
                schema: "dbo",
                table: "companies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "representative_user_id",
                schema: "dbo",
                table: "companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "territorial_level",
                schema: "dbo",
                table: "companies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_dbo_user_profiles_country_id",
                schema: "dbo",
                table: "user_profiles",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_user_profiles_department_id",
                schema: "dbo",
                table: "user_profiles",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_user_profiles_municipality_id",
                schema: "dbo",
                table: "user_profiles",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_country_id",
                schema: "dbo",
                table: "companies",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_created_by_user_id",
                schema: "dbo",
                table: "companies",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_department_id",
                schema: "dbo",
                table: "companies",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_last_modified_by_user_id",
                schema: "dbo",
                table: "companies",
                column: "last_modified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_municipality_id",
                schema: "dbo",
                table: "companies",
                column: "municipality_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_representative_document_type_id",
                schema: "dbo",
                table: "companies",
                column: "representative_document_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_companies_representative_user_id",
                schema: "dbo",
                table: "companies",
                column: "representative_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_companies_countries_country_id",
                schema: "dbo",
                table: "companies",
                column: "country_id",
                principalSchema: "param",
                principalTable: "countries",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_companies_departments_department_id",
                schema: "dbo",
                table: "companies",
                column: "department_id",
                principalSchema: "param",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_companies_document_types_representative_document_type_id",
                schema: "dbo",
                table: "companies",
                column: "representative_document_type_id",
                principalSchema: "param",
                principalTable: "document_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_companies_municipalities_municipality_id",
                schema: "dbo",
                table: "companies",
                column: "municipality_id",
                principalSchema: "param",
                principalTable: "municipalities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_companies_user_accounts_created_by_user_id",
                schema: "dbo",
                table: "companies",
                column: "created_by_user_id",
                principalSchema: "dbo",
                principalTable: "user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_companies_user_accounts_last_modified_by_user_id",
                schema: "dbo",
                table: "companies",
                column: "last_modified_by_user_id",
                principalSchema: "dbo",
                principalTable: "user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_companies_user_accounts_representative_user_id",
                schema: "dbo",
                table: "companies",
                column: "representative_user_id",
                principalSchema: "dbo",
                principalTable: "user_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_user_profiles_countries_country_id",
                schema: "dbo",
                table: "user_profiles",
                column: "country_id",
                principalSchema: "param",
                principalTable: "countries",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_user_profiles_departments_department_id",
                schema: "dbo",
                table: "user_profiles",
                column: "department_id",
                principalSchema: "param",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_dbo_user_profiles_municipalities_municipality_id",
                schema: "dbo",
                table: "user_profiles",
                column: "municipality_id",
                principalSchema: "param",
                principalTable: "municipalities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_countries_country_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_departments_department_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_document_types_representative_document_type_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_municipalities_municipality_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_user_accounts_created_by_user_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_user_accounts_last_modified_by_user_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_companies_user_accounts_representative_user_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_user_profiles_countries_country_id",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_user_profiles_departments_department_id",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_dbo_user_profiles_municipalities_municipality_id",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.DropIndex(
                name: "IX_dbo_user_profiles_country_id",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.DropIndex(
                name: "IX_dbo_user_profiles_department_id",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.DropIndex(
                name: "IX_dbo_user_profiles_municipality_id",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.DropIndex(
                name: "IX_dbo_companies_country_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_dbo_companies_created_by_user_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_dbo_companies_department_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_dbo_companies_last_modified_by_user_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_dbo_companies_municipality_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_dbo_companies_representative_document_type_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_dbo_companies_representative_user_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "country_id",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "department_id",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "municipality_id",
                schema: "dbo",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "company_type",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "country_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "department_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "last_modified_at",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "last_modified_by_user_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "municipality_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "representative_document_number",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "representative_document_type_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "representative_full_name",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "representative_mode",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "representative_user_id",
                schema: "dbo",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "territorial_level",
                schema: "dbo",
                table: "companies");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "dbo",
                table: "companies",
                newName: "created_at_utc");

            migrationBuilder.AddColumn<string>(
                name: "residence_city",
                schema: "dbo",
                table: "user_profiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "residence_country",
                schema: "dbo",
                table: "user_profiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "residence_department",
                schema: "dbo",
                table: "user_profiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.RenameColumn(
                name: "last_modified_at",
                schema: "dbo",
                table: "companies",
                newName: "updated_at_utc");
        }
    }
}
