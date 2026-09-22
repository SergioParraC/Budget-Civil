namespace BudgetCivil.Domain.Users.Entities;

public sealed class UserAccount
{
    public Guid Id { get; set; }

    public Guid? CompanyId { get; set; }

    public Guid RoleId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool AccountEnabled { get; set; } = true;

    public bool AccountLocked { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Company? Company { get; set; }

    public Role Role { get; set; } = null!;

    public UserProfile? Profile { get; set; }

    public ICollection<UserCompanyMembership> Memberships { get; set; } = new List<UserCompanyMembership>();

    public ICollection<PasswordRecoveryGrant> RecoveryGrants { get; set; } = new List<PasswordRecoveryGrant>();

    public ICollection<PasswordRecoveryGrant> EnabledRecoveryGrants { get; set; } = new List<PasswordRecoveryGrant>();

    public ICollection<SecurityAuditEvent> SecurityAuditEvents { get; set; } = new List<SecurityAuditEvent>();
}
