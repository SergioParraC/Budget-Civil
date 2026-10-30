using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Notifications;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Catalogs;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Users.Auth.Dtos;
using BudgetCivil.Application.Users.Auth.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Users.Auth.UseCases;

public sealed class RegisterUserUseCase(
    IAuthRepository authRepository,
    IEmailVerificationTokenRepository emailVerificationTokenRepository,
    IGeographicCatalogRepository geographicCatalogRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IValidator<RegisterUserRequest> validator)
{
    private const string SelfRegistrationRoleCode = "PRESUPUESTADOR";

    // D-80: la cuenta queda deshabilitada hasta verificar el correo; el token caduca en 24 horas.
    private static readonly TimeSpan VerificationTokenLifetime = TimeSpan.FromHours(24);

    public async Task<RegisterUserResponse> ExecuteAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        await GeographicLocationRules.EnsureValidActiveLocationAsync(
            geographicCatalogRepository,
            request.CountryId,
            request.DepartmentId,
            request.MunicipalityId,
            cancellationToken);

        if (await authRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ConflictAppException("Email already exists.", "EMAIL_ALREADY_EXISTS");
        }

        if (await authRepository.UsernameExistsAsync(request.Username, cancellationToken))
        {
            throw new ConflictAppException("Username already exists.", "USERNAME_ALREADY_EXISTS");
        }

        var role = await authRepository.GetRoleByCodeAsync(SelfRegistrationRoleCode, cancellationToken);
        if (role is null || !role.IsActive)
        {
            throw new ConflictAppException("Default self-registration role is not available.", "DEFAULT_ROLE_NOT_AVAILABLE");
        }

        var utcNow = DateTime.UtcNow;
        var userAccount = new UserAccount
        {
            Id = Guid.NewGuid(),
            CompanyId = null,
            RoleId = role.Id,
            Email = request.Email.Trim(),
            Username = request.Username.Trim(),
            PasswordHash = passwordHasher.HashPassword(request.Password),
            AccountEnabled = false,
            AccountLocked = false,
            FailedLoginAttempts = 0,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            FirstName = request.FirstName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            LastName = request.LastName.Trim(),
            SecondLastName = string.IsNullOrWhiteSpace(request.SecondLastName) ? null : request.SecondLastName.Trim(),
            DocumentType = request.DocumentType.Trim(),
            DocumentNumber = request.DocumentNumber.Trim(),
            BirthDate = request.BirthDate,
            CountryId = request.CountryId,
            DepartmentId = request.DepartmentId,
            MunicipalityId = request.MunicipalityId
        };

        var rawVerificationToken = TokenHash.CreateRawToken();
        var verificationToken = new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = userAccount.Id,
            TokenHash = TokenHash.Compute(rawVerificationToken),
            ExpiresAtUtc = utcNow.Add(VerificationTokenLifetime),
            CreatedAtUtc = utcNow
        };

        await authRepository.AddUserAsync(userAccount, cancellationToken);
        await emailVerificationTokenRepository.AddTokenAsync(verificationToken, cancellationToken);
        await emailVerificationTokenRepository.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            userAccount.Email,
            "Verifica tu cuenta en Budget Civil",
            $"Tu cuenta fue creada. Verifica tu correo con este codigo; caduca en 24 horas: {rawVerificationToken}",
            cancellationToken);

        return new RegisterUserResponse
        {
            UserId = userAccount.Id,
            AssignedRole = role.Name,
            CompanyId = null,
            AccountEnabled = userAccount.AccountEnabled,
            CreatedAtUtc = userAccount.CreatedAtUtc
        };
    }

    private async Task ValidateAsync(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid)
        {
            return;
        }

        var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
        throw new ValidationAppException(message, "VALIDATION_ERROR");
    }
}
