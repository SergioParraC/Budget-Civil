namespace BudgetCivil.Domain.Users.Entities;

public sealed class UserSignature
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string SignatureName { get; set; } = string.Empty;

    public string SignatureImageBase64 { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}