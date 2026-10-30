using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Users.Auth.Repositories;

namespace BudgetCivil.Application.Users.Auth.UseCases;

/// <summary>
/// `REQ-IDENTIDAD-07` (`D-80`): consume el token de verificación una sola vez y habilita la cuenta. La
/// cuenta autorregistrada permanece deshabilitada hasta este paso.
/// </summary>
public sealed class ConfirmEmailVerificationUseCase(IEmailVerificationTokenRepository repository)
{
    public async Task ExecuteAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ValidationAppException("token is required.", "VALIDATION_ERROR");
        }

        var token = await repository.FindTokenByHashAsync(TokenHash.Compute(rawToken.Trim()), cancellationToken);

        if (token is null || token.ConsumedAtUtc is not null || token.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new ValidationAppException(
                "Email verification token is invalid or expired.",
                "EMAIL_VERIFICATION_TOKEN_INVALID");
        }

        var utcNow = DateTime.UtcNow;
        token.UserAccount.AccountEnabled = true;
        token.UserAccount.UpdatedAtUtc = utcNow;
        token.ConsumedAtUtc = utcNow;

        await repository.SaveChangesAsync(cancellationToken);
    }
}
