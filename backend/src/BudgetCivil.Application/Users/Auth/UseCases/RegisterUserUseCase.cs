using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Users.Auth.Dtos;
using BudgetCivil.Application.Users.Auth.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Users.Auth.UseCases;

public sealed class RegisterUserUseCase(
    IAuthRepository authRepository,
    IPasswordHasher passwordHasher,
    IValidator<RegisterUserRequest> validator)
{
    private const string SelfRegistrationRoleCode = "PRESUPUESTADOR";

    public async Task<RegisterUserResponse> ExecuteAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        if (await authRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            throw new ConflictAppException("Email already exists.", "EMAIL_ALREADY_EXISTS");
        }

        if (await authRepository.UsernameExistsAsync(request.Username, cancellationToken))
        {
            throw new ConflictAppException("Username already exists.", "USERNAME_ALREADY_EXISTS");
        }

        if (await authRepository.DocumentExistsAsync(request.DocumentType, request.DocumentNumber, cancellationToken))
        {
            throw new ConflictAppException("Document already exists.", "DOCUMENT_ALREADY_EXISTS");
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
            AccountEnabled = true,
            AccountLocked = false,
            FailedLoginAttempts = 0,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            Profile = new UserProfile
            {
                UserId = Guid.Empty,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                DocumentType = request.DocumentType.Trim(),
                DocumentNumber = request.DocumentNumber.Trim(),
                BirthDate = new DateOnly(2000, 1, 1),
                ResidenceCity = "PENDING",
                ResidenceDepartment = "PENDING",
                ResidenceCountry = "PENDING"
            }
        };

        userAccount.Profile.UserId = userAccount.Id;

        await authRepository.AddUserAsync(userAccount, cancellationToken);

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
