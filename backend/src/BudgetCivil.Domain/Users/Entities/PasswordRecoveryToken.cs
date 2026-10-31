namespace BudgetCivil.Domain.Users.Entities;

/// <summary>RF-18: token de un solo uso para la recuperacion de contrasena por correo (REQ-IDENTIDAD-25).</summary>
public sealed class PasswordRecoveryToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public UserAccount UserAccount { get; set; } = null!;
}