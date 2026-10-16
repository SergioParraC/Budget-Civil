using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Catalogs;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Users.Administration.Dtos;
using BudgetCivil.Application.Users.Administration.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Users.Administration.UseCases;

public sealed class CreateUserUseCase(
    IUserAdministrationRepository repository,
    IGeographicCatalogRepository geographicCatalogRepository,
    IPasswordHasher passwordHasher,
    ISecurityAuditService securityAuditService,
    IValidator<CreateUserRequest> validator)
{
    private const string DefaultAssignedRoleCode = "PRESUPUESTADOR";

    public async Task<CreateUserResponse> ExecuteAsync(
        Guid actorUserId,
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        AdministrationAuthorizationRules.EnsureActorCanCreateUsers(actor);

        await GeographicLocationRules.EnsureValidActiveLocationAsync(
            geographicCatalogRepository,
            request.Residence.CountryId,
            request.Residence.DepartmentId,
            request.Residence.MunicipalityId,
            cancellationToken);

        if (await repository.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
        {
            throw new ConflictAppException("Email already exists.", "EMAIL_ALREADY_EXISTS");
        }

        if (await repository.UsernameExistsAsync(request.Username, cancellationToken: cancellationToken))
        {
            throw new ConflictAppException("Username already exists.", "USERNAME_ALREADY_EXISTS");
        }

        if (await repository.DocumentExistsAsync(request.DocumentType, request.DocumentNumber, cancellationToken: cancellationToken))
        {
            throw new ConflictAppException("Document already exists.", "DOCUMENT_ALREADY_EXISTS");
        }

        var requestedRoleCode = string.IsNullOrWhiteSpace(request.AssignedRoleCode)
            ? DefaultAssignedRoleCode
            : request.AssignedRoleCode.Trim().ToUpperInvariant();

        var assignedRole = await repository.GetRoleByCodeAsync(requestedRoleCode, cancellationToken);
        if (assignedRole is null || !assignedRole.IsActive)
        {
            throw new ValidationAppException("Assigned role is not available.", "ASSIGNED_ROLE_NOT_AVAILABLE");
        }

        Company? assignedCompany = null;
        if (request.CompanyId.HasValue)
        {
            assignedCompany = await repository.GetActiveCompanyByIdAsync(request.CompanyId.Value, cancellationToken);
            if (assignedCompany is null)
            {
                throw new ValidationAppException("Company does not exist or is inactive.", "COMPANY_NOT_AVAILABLE");
            }
        }

        var utcNow = DateTime.UtcNow;
        var userId = Guid.NewGuid();

        var userAccount = new UserAccount
        {
            Id = userId,
            CompanyId = assignedCompany?.Id,
            RoleId = assignedRole.Id,
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
                UserId = userId,
                FirstName = request.FirstName.Trim(),
                MiddleName = NormalizeOptionalText(request.MiddleName),
                LastName = request.LastName.Trim(),
                SecondLastName = NormalizeOptionalText(request.SecondLastName),
                DocumentType = request.DocumentType.Trim(),
                DocumentNumber = request.DocumentNumber.Trim(),
                BirthDate = request.BirthDate,
                CountryId = request.Residence.CountryId,
                DepartmentId = request.Residence.DepartmentId,
                MunicipalityId = request.Residence.MunicipalityId,
                ProfessionalCard = NormalizeOptionalText(request.ProfessionalCard),
                ProfessionalCardIssuedAt = request.ProfessionalCardIssuedAt,
                ProfessionalCardFilePath = NormalizeOptionalText(request.ProfessionalCardFilePath)
            }
        };

        await repository.AddUserAsync(userAccount, cancellationToken);

        if (assignedCompany is not null)
        {
            await repository.AddMembershipAsync(
                new UserCompanyMembership
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CompanyId = assignedCompany.Id,
                    IsActive = true,
                    AssignedByUserId = actor!.Id,
                    AssignedAtUtc = utcNow
                },
                cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor!.Id,
                actor.CompanyId,
                SecurityAuditEventType.UserAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "USERS_CREATE",
                TargetType: nameof(UserAccount),
                TargetId: userId,
                ReasonCode: "USER_CREATED",
                Metadata: new Dictionary<string, string>
                {
                    ["assigned_role_code"] = assignedRole.Code,
                    ["assigned_company_id"] = assignedCompany?.Id.ToString() ?? "null"
                }),
            cancellationToken);

        return new CreateUserResponse
        {
            UserId = userId,
            AssignedRole = assignedRole.Name,
            CompanyId = assignedCompany?.Id,
            AccountEnabled = true,
            CreatedAtUtc = utcNow
        };
    }

    private async Task ValidateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid)
        {
            return;
        }

        var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
        throw new ValidationAppException(message, "VALIDATION_ERROR");
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
