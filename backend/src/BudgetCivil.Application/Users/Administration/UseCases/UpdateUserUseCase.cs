using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Catalogs;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Users.Administration.Dtos;
using BudgetCivil.Application.Users.Administration.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Users.Administration.UseCases;

public sealed class UpdateUserUseCase(
    IUserAdministrationRepository repository,
    IGeographicCatalogRepository geographicCatalogRepository,
    ISecurityAuditService securityAuditService,
    IValidator<UpdateUserRequest> validator)
{
    public async Task<UpdateUserResponse> ExecuteAsync(
        Guid actorUserId,
        Guid targetUserId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);
        await GeographicLocationRules.EnsureValidActiveLocationAsync(
            geographicCatalogRepository,
            request.Residence.CountryId,
            request.Residence.DepartmentId,
            request.Residence.MunicipalityId,
            cancellationToken);

        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        var targetUser = await repository.GetUserForManagementAsync(targetUserId, cancellationToken);

        AdministrationAuthorizationRules.EnsureActorCanManageTarget(actor, targetUser);

        if (await repository.EmailExistsAsync(request.Email, targetUserId, cancellationToken))
        {
            throw new ConflictAppException("Email already exists.", "EMAIL_ALREADY_EXISTS");
        }

        if (await repository.UsernameExistsAsync(request.Username, targetUserId, cancellationToken))
        {
            throw new ConflictAppException("Username already exists.", "USERNAME_ALREADY_EXISTS");
        }

        var utcNow = DateTime.UtcNow;

        targetUser!.Email = request.Email.Trim();
        targetUser.Username = request.Username.Trim();
        targetUser.UpdatedAtUtc = utcNow;

        targetUser.FirstName = request.FirstName.Trim();
        targetUser.MiddleName = NormalizeOptionalText(request.MiddleName);
        targetUser.LastName = request.LastName.Trim();
        targetUser.SecondLastName = NormalizeOptionalText(request.SecondLastName);
        targetUser.DocumentType = request.DocumentType.Trim();
        targetUser.DocumentNumber = request.DocumentNumber.Trim();
        targetUser.BirthDate = request.BirthDate;
        targetUser.CountryId = request.Residence.CountryId;
        targetUser.DepartmentId = request.Residence.DepartmentId;
        targetUser.MunicipalityId = request.Residence.MunicipalityId;
        targetUser.ProfessionalCard = NormalizeOptionalText(request.ProfessionalCard);
        targetUser.ProfessionalCardIssuedAt = request.ProfessionalCardIssuedAt;
        targetUser.ProfessionalCardFilePath = NormalizeOptionalText(request.ProfessionalCardFilePath);

        await repository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor!.Id,
                actor.CompanyId,
                SecurityAuditEventType.UserAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "USERS_UPDATE",
                TargetType: nameof(UserAccount),
                TargetId: targetUser.Id,
                ReasonCode: "USER_UPDATED"),
            cancellationToken);

        return new UpdateUserResponse
        {
            UserId = targetUser.Id,
            UpdatedAtUtc = utcNow
        };
    }

    private async Task ValidateAsync(UpdateUserRequest request, CancellationToken cancellationToken)
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
