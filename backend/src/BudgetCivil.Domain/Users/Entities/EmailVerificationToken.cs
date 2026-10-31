namespace BudgetCivil.Domain.Users.Entities;

/// <summary>
/// `D-80`: token de un solo uso para verificar el correo declarado en el autorregistro. La cuenta no se
/// habilita hasta que este token se consume (`REQ-IDENTIDAD-07`).
/// </summary>
public sealed class EmailVerificationToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public UserAccount UserAccount { get; set; } = null!;
}
