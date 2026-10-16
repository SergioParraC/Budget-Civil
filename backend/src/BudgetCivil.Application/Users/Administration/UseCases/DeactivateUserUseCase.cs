using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Users.Administration.Dtos;
using BudgetCivil.Application.Users.Administration.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Users.Administration.UseCases;

public sealed class DeactivateUserUseCase(
    IUserAdministrationRepository repository,
    ISecurityAuditService securityAuditService,
    IValidator<DeactivateUserRequest> validator)
{
    public async Task<DeactivateUserResponse> ExecuteAsync(
        Guid actorUserId,
        Guid targetUserId,
        DeactivateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        var targetUser = await repository.GetUserForManagementAsync(targetUserId, cancellationToken);

        AdministrationAuthorizationRules.EnsureActorCanManageTarget(actor, targetUser);

        if (targetUser!.Role.Code == AdministrationAuthorizationRules.AdministratorRoleCode && targetUser.AccountEnabled)
        {
            var enabledAdministratorCount = await repository.CountEnabledAdministratorsAsync(cancellationToken);
            if (enabledAdministratorCount <= 1)
            {
                throw new ConflictAppException(
                    "Cannot deactivate the last active administrator.",
                    "LAST_ACTIVE_ADMINISTRATOR_CONFLICT");
            }
        }

        targetUser.AccountEnabled = false;
        targetUser.UpdatedAtUtc = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor!.Id,
                actor.CompanyId,
                SecurityAuditEventType.UserAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "USERS_DEACTIVATE",
                TargetType: nameof(UserAccount),
                TargetId: targetUser.Id,
                ReasonCode: "USER_DEACTIVATED",
                Metadata: new Dictionary<string, string>
                {
                    ["reason"] = request.Reason.Trim()
                }),
            cancellationToken);

        return new DeactivateUserResponse
        {
            UserId = targetUser.Id,
            AccountEnabled = targetUser.AccountEnabled
        };
    }

    private async Task ValidateAsync(DeactivateUserRequest request, CancellationToken cancellationToken)
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
