namespace BudgetCivil.Domain.Users.Entities;

public sealed class Company
{
    public Guid Id { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string TaxId { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<UserAccount> UserAccounts { get; set; } = new List<UserAccount>();

    public ICollection<UserCompanyMembership> Memberships { get; set; } = new List<UserCompanyMembership>();
}
