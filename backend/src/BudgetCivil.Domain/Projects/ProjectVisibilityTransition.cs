namespace BudgetCivil.Domain.Projects;

public sealed class ProjectVisibilityTransition
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public bool PreviousIsPublic { get; set; }

    public bool NewIsPublic { get; set; }

    public Guid ChangedByUserId { get; set; }

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    public string? Reason { get; set; }
}
