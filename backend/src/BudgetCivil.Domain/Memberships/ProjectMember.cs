namespace BudgetCivil.Domain.Memberships;

/// <summary>
/// Pertenencia de un usuario a un proyecto (`D-98`): una sola fila por (`project_id`, `user_id`) y sin
/// historial de asignaciones (`D-08`). La compañía del usuario vive en `User.company_id`.
/// </summary>
public sealed class ProjectMember
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }

    public string RoleInProject { get; set; } = string.Empty;

    public Guid AssignedByUserId { get; set; }

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
