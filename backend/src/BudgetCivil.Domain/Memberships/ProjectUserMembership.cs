namespace BudgetCivil.Domain.Memberships;

public sealed class ProjectUserMembership
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid AssignedByUserId { get; set; }

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAtUtc { get; set; }
}
