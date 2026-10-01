namespace BudgetCivil.Domain.Projects;

public sealed class ProjectStatusTransition
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public ProjectStatus PreviousStatus { get; set; }

    public ProjectStatus NewStatus { get; set; }

    public Guid ChangedByUserId { get; set; }

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    public string? Reason { get; set; }

    public ProjectTransitionKind TransitionKind { get; set; }
}
