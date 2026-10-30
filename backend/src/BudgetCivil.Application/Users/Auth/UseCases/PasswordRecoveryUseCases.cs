using System.Security.Cryptography;
using System.Text;
using BudgetCivil.Application.Common.Notifications;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Users.Auth.Repositories;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Users.Auth.UseCases;

/// <summary>
/// REQ-IDENTIDAD-25: la solicitud no revela si el correo existe y siempre responde igual. El token se
/// guarda como hash SHA-256 y caduca en una hora.
/// </summary>
public sealed class RequestPasswordRecoveryUseCase(
    IPasswordRecoveryRepository repository,
    IEmailSender emailSender)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public async Task ExecuteAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await repository.FindUserByEmailAsync(email, cancellationToken);
        if (user is null || !user.AccountEnabled || user.AccountLocked)
        {
            return;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var token = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = DateTime.UtcNow.Add(TokenLifetime),
            CreatedAtUtc = DateTime.UtcNow
        };

        await repository.AddTokenAsync(token, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            user.Email,
            "Recuperacion de contrasena",
            $"Usa este codigo para restablecer tu contrasena. Caduca en una hora: {rawToken}",
            cancellationToken);
    }

    public static string Hash(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}

/// <summary>REQ-IDENTIDAD-25: consume el token una sola vez y aplica la politica de contrasena vigente.</summary>
public sealed class ConfirmPasswordRecoveryUseCase(
    IPasswordRecoveryRepository repository,
    IPasswordHasher passwordHasher)
{
    public async Task ExecuteAsync(string rawToken, string newPassword, CancellationToken cancellationToken = default)
    {
        var token = await repository.FindTokenByHashAsync(RequestPasswordRecoveryUseCase.Hash(rawToken.Trim()), cancellationToken);

        if (token is null || token.ConsumedAtUtc is not null || token.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new Common.Exceptions.ValidationAppException(
                "Recovery token is invalid or expired.",
                "RECOVERY_TOKEN_INVALID");
        }

        token.UserAccount.PasswordHash = passwordHasher.HashPassword(newPassword);
        token.UserAccount.AccountLocked = false;
        token.UserAccount.FailedLoginAttempts = 0;
        token.UserAccount.UpdatedAtUtc = DateTime.UtcNow;
        token.ConsumedAtUtc = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);
    }
}