using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class wbs_add_i2b : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wbs_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "varchar(10)", nullable: false, collation: "Latin1_General_100_CI_AS"),
                    node_type = table.Column<string>(type: "varchar(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_wbs_codes", x => x.id);
                    table.CheckConstraint("CK_wbs_codes_code_length", "LEN(code) BETWEEN 1 AND 10");
                    table.CheckConstraint("CK_wbs_codes_node_type", "node_type IN ('CHAPTER','ITEM')");
                    table.ForeignKey(
                        name: "FK_dbo_wbs_codes_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wbs_project_activity",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_modified_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_wbs_project_activity", x => new { x.project_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_dbo_wbs_project_activity_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_wbs_project_activity_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wbs_chapters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    parent_chapter_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    code_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_modified_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_wbs_chapters", x => x.id);
                    table.CheckConstraint("CK_wbs_chapters_order_index", "order_index >= 0");
                    table.ForeignKey(
                        name: "FK_dbo_wbs_chapters_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_wbs_chapters_users_last_modified_by",
                        column: x => x.last_modified_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_wbs_chapters_wbs_chapters_parent_chapter_id",
                        column: x => x.parent_chapter_id,
                        principalTable: "wbs_chapters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_wbs_chapters_wbs_codes_code_id",
                        column: x => x.code_id,
                        principalTable: "wbs_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "wbs_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    project_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    wbs_chapter_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    apu_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    code_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    quantity_work = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_modified_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dbo_wbs_items", x => x.id);
                    table.CheckConstraint("CK_wbs_items_order_index", "order_index >= 0");
                    table.CheckConstraint("CK_wbs_items_quantity_work", "quantity_work > 0");
                    table.ForeignKey(
                        name: "FK_dbo_wbs_items_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_wbs_items_users_last_modified_by",
                        column: x => x.last_modified_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_wbs_items_wbs_chapters_wbs_chapter_id",
                        column: x => x.wbs_chapter_id,
                        principalTable: "wbs_chapters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_dbo_wbs_items_wbs_codes_code_id",
                        column: x => x.code_id,
                        principalTable: "wbs_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_wbs_chapters_last_modified_by",
                table: "wbs_chapters",
                column: "last_modified_by");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_wbs_chapters_parent_chapter_id",
                table: "wbs_chapters",
                column: "parent_chapter_id");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_wbs_chapters_project_id_parent_chapter_id_order_index",
                table: "wbs_chapters",
                columns: new[] { "project_id", "parent_chapter_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_wbs_chapters_code_id",
                table: "wbs_chapters",
                column: "code_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_wbs_codes_project_id_code",
                table: "wbs_codes",
                columns: new[] { "project_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dbo_wbs_items_last_modified_by",
                table: "wbs_items",
                column: "last_modified_by");

            migrationBuilder.CreateIndex(
                name: "IX_dbo_wbs_items_project_id_wbs_chapter_id_order_index",
                table: "wbs_items",
                columns: new[] { "project_id", "wbs_chapter_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_wbs_items_wbs_chapter_id",
                table: "wbs_items",
                column: "wbs_chapter_id");

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_wbs_items_apu_id",
                table: "wbs_items",
                column: "apu_id",
                unique: true,
                filter: "[apu_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_dbo_wbs_items_code_id",
                table: "wbs_items",
                column: "code_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dbo_wbs_project_activity_project_id_last_modified_at",
                table: "wbs_project_activity",
                columns: new[] { "project_id", "last_modified_at" });

            migrationBuilder.CreateIndex(
                name: "IX_dbo_wbs_project_activity_user_id",
                table: "wbs_project_activity",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wbs_items");

            migrationBuilder.DropTable(
                name: "wbs_project_activity");

            migrationBuilder.DropTable(
                name: "wbs_chapters");

            migrationBuilder.DropTable(
                name: "wbs_codes");
        }
    }
}
