namespace BudgetCivil.Domain.Users.Entities;

public sealed class UserCompanyMembership
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CompanyId { get; set; }

    public bool IsActive { get; set; }

    public Guid AssignedByUserId { get; set; }

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAtUtc { get; set; }

    public UserAccount UserAccount { get; set; } = null!;

    public Company Company { get; set; } = null!;

    public UserAccount AssignedByUser { get; set; } = null!;
}
