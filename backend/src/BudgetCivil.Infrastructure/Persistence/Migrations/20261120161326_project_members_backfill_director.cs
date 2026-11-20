using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetCivil.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class project_members_backfill_director : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // INC-20: los proyectos creados antes de este cambio no registraban al Director responsable
            // en `project_members` y por eso el EDT/APUs respondían 403 WBS_WRITE_FORBIDDEN/APU_WRITE_FORBIDDEN.
            migrationBuilder.Sql(
                """
                INSERT INTO project_members (id, project_id, user_id, role_in_project, assigned_by_user_id, assigned_at_utc)
                SELECT NEWID(), p.id, p.director_owner_user_id, N'DIRECTOR', p.director_owner_user_id, SYSUTCDATETIME()
                FROM projects p
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM project_members pm
                    WHERE pm.project_id = p.id AND pm.user_id = p.director_owner_user_id
                );

                INSERT INTO project_members (id, project_id, user_id, role_in_project, assigned_by_user_id, assigned_at_utc)
                SELECT NEWID(), p.id, p.created_by_user_id, N'PRESUPUESTADOR', p.created_by_user_id, SYSUTCDATETIME()
                FROM projects p
                INNER JOIN users u ON u.id = p.created_by_user_id
                INNER JOIN roles r ON r.id = u.role_id
                WHERE r.code = N'PRESUPUESTADOR'
                  AND p.created_by_user_id <> p.director_owner_user_id
                  AND NOT EXISTS (
                      SELECT 1
                      FROM project_members pm
                      WHERE pm.project_id = p.id AND pm.user_id = p.created_by_user_id
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin reversión: las filas insertadas son pertenencias válidas y eliminarlas volvería a
            // bloquear la escritura del EDT y de los APUs para los responsables de los proyectos.
        }
    }
}
