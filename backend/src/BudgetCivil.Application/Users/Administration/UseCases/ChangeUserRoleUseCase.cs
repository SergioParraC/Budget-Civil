using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Users.Administration.Dtos;
using BudgetCivil.Application.Users.Administration.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Users.Administration.UseCases;

public sealed class ChangeUserRoleUseCase(
    IUserAdministrationRepository repository,
    ISecurityAuditService securityAuditService,
    IValidator<ChangeUserRoleRequest> validator)
{
    public async Task<ChangeUserRoleResponse> ExecuteAsync(
        Guid actorUserId,
        Guid targetUserId,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        var targetUser = await repository.GetUserForManagementAsync(targetUserId, cancellationToken);

        AdministrationAuthorizationRules.EnsureActorCanManageTarget(actor, targetUser);

        var requestedRoleCode = request.AssignedRoleCode.Trim().ToUpperInvariant();
        AdministrationAuthorizationRules.EnsureManagerCannotElevateToAdministrator(actor!, requestedRoleCode);

        var requestedRole = await repository.GetRoleByCodeAsync(requestedRoleCode, cancellationToken);
        if (requestedRole is null || !requestedRole.IsActive)
        {
            throw new ValidationAppException("Assigned role is not available.", "ASSIGNED_ROLE_NOT_AVAILABLE");
        }

        var previousRoleCode = targetUser!.Role.Code;
        targetUser.RoleId = requestedRole.Id;
        targetUser.UpdatedAtUtc = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor!.Id,
                actor.CompanyId,
                SecurityAuditEventType.UserAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "USERS_ROLE_CHANGE",
                TargetType: nameof(UserAccount),
                TargetId: targetUser.Id,
                ReasonCode: "USER_ROLE_CHANGED",
                Metadata: new Dictionary<string, string>
                {
                    ["previous_role_code"] = previousRoleCode,
                    ["new_role_code"] = requestedRole.Code
                }),
            cancellationToken);

        return new ChangeUserRoleResponse
        {
            UserId = targetUser.Id,
            AssignedRole = requestedRole.Name
        };
    }

    private async Task ValidateAsync(ChangeUserRoleRequest request, CancellationToken cancellationToken)
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
