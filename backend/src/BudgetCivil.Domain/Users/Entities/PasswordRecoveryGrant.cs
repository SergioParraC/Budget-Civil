namespace BudgetCivil.Domain.Users.Entities;

public sealed class PasswordRecoveryGrant
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid EnabledByUserId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime EnabledAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? ConsumedAtUtc { get; set; }

    public UserAccount UserAccount { get; set; } = null!;

    public UserAccount EnabledByUser { get; set; } = null!;
}
