using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Users.Administration.Dtos;
using BudgetCivil.Application.Users.Administration.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Users.Administration.UseCases;

public sealed class ChangeUserCompanyUseCase(
    IUserAdministrationRepository repository,
    ISecurityAuditService securityAuditService,
    IValidator<ChangeUserCompanyRequest> validator)
{
    public async Task<ChangeUserCompanyResponse> ExecuteAsync(
        Guid actorUserId,
        Guid targetUserId,
        ChangeUserCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        var targetUser = await repository.GetUserForManagementAsync(targetUserId, cancellationToken);

        AdministrationAuthorizationRules.EnsureActorCanAssignCompany(actor, targetUser, request.CompanyId);

        var destinationCompany = await repository.GetActiveCompanyByIdAsync(request.CompanyId, cancellationToken);
        if (destinationCompany is null)
        {
            throw new ValidationAppException("Company does not exist or is inactive.", "COMPANY_NOT_AVAILABLE");
        }

        var activeMembership = await repository.GetActiveMembershipAsync(targetUserId, cancellationToken);
        if (activeMembership is not null || targetUser!.CompanyId.HasValue)
        {
            throw new ConflictAppException(
                "User already belongs to an active company.",
                "ACTIVE_MEMBERSHIP_ALREADY_EXISTS");
        }

        targetUser.CompanyId = destinationCompany.Id;
        targetUser.UpdatedAtUtc = DateTime.UtcNow;

        await repository.AddMembershipAsync(
            new UserCompanyMembership
            {
                Id = Guid.NewGuid(),
                UserId = targetUserId,
                CompanyId = destinationCompany.Id,
                IsActive = true,
                AssignedByUserId = actor!.Id,
                AssignedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor!.Id,
                actor.CompanyId,
                SecurityAuditEventType.UserAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "USERS_COMPANY_CHANGE",
                TargetType: nameof(UserAccount),
                TargetId: targetUserId,
                ReasonCode: "USER_COMPANY_CHANGED",
                Metadata: new Dictionary<string, string>
                {
                    ["company_id"] = destinationCompany.Id.ToString()
                }),
            cancellationToken);

        return new ChangeUserCompanyResponse
        {
            UserId = targetUserId,
            CompanyId = destinationCompany.Id
        };
    }

    private async Task ValidateAsync(ChangeUserCompanyRequest request, CancellationToken cancellationToken)
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
