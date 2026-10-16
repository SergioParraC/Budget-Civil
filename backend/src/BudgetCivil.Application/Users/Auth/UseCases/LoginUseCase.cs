using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Users.Auth.Dtos;
using BudgetCivil.Application.Users.Auth.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Users.Auth.UseCases;

public sealed class LoginUseCase(
    IAuthRepository authRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ISecurityAuditService securityAuditService,
    IValidator<LoginRequest> validator)
{
    private const int MaxFailedAttempts = 5;
    private const string AccountNotEnabledMessage = "Este no se encuentra habilitado, solicite al administrador su habilitacion";

    public async Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var userAccount = await authRepository.FindByIdentifierAsync(request.Identifier, cancellationToken);
        if (userAccount is null)
        {
            throw new UnauthorizedAppException("Credenciales invalidas.", "INVALID_CREDENTIALS");
        }

        if (!userAccount.AccountEnabled || userAccount.AccountLocked)
        {
            throw new LockedAppException(AccountNotEnabledMessage, "ACCOUNT_NOT_ENABLED");
        }

        if (!passwordHasher.VerifyHashedPassword(userAccount.PasswordHash, request.Password))
        {
            await ProcessInvalidPasswordAsync(userAccount, cancellationToken);
            throw new UnauthorizedAppException("Credenciales invalidas.", "INVALID_CREDENTIALS");
        }

        userAccount.FailedLoginAttempts = 0;
        userAccount.LastLoginAtUtc = DateTime.UtcNow;
        userAccount.UpdatedAtUtc = DateTime.UtcNow;

        await authRepository.SaveChangesAsync(cancellationToken);

        var permissionKeys = await authRepository.GetActivePermissionKeysAsync(userAccount.RoleId, cancellationToken);
        var accessToken = jwtTokenService.GenerateToken(userAccount, permissionKeys);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                userAccount.Id,
                userAccount.CompanyId,
                SecurityAuditEventType.AuthLogin,
                SecurityAuditResult.Success,
                Metadata: new Dictionary<string, string>
                {
                    ["identifier"] = request.Identifier.Trim()
                }),
            cancellationToken);

        return new LoginResponse
        {
            AccessToken = accessToken.AccessToken,
            TokenType = accessToken.TokenType,
            ExpiresAtUtc = accessToken.ExpiresAtUtc,
            User = new LoginUserPayload
            {
                UserId = userAccount.Id,
                Email = userAccount.Email,
                Username = userAccount.Username,
                Role = userAccount.Role.Name,
                CompanyId = userAccount.CompanyId,
                AccountEnabled = userAccount.AccountEnabled
            }
        };
    }

    private async Task ValidateAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid)
        {
            return;
        }

        var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
        throw new ValidationAppException(message, "VALIDATION_ERROR");
    }

    private async Task ProcessInvalidPasswordAsync(UserAccount userAccount, CancellationToken cancellationToken)
    {
        userAccount.FailedLoginAttempts += 1;
        userAccount.UpdatedAtUtc = DateTime.UtcNow;

        if (userAccount.FailedLoginAttempts >= MaxFailedAttempts)
        {
            userAccount.AccountLocked = true;
            await authRepository.SaveChangesAsync(cancellationToken);

            await securityAuditService.WriteAsync(
                new SecurityAuditWriteRequest(
                    userAccount.Id,
                    userAccount.CompanyId,
                    SecurityAuditEventType.AuthLockout,
                    SecurityAuditResult.Failed,
                    ReasonCode: "FAILED_LOGIN_ATTEMPTS_LIMIT",
                    Metadata: new Dictionary<string, string>
                    {
                        ["failed_attempts"] = userAccount.FailedLoginAttempts.ToString()
                    }),
                cancellationToken);

            throw new LockedAppException(AccountNotEnabledMessage, "ACCOUNT_NOT_ENABLED");
        }

        await authRepository.SaveChangesAsync(cancellationToken);
    }
}
