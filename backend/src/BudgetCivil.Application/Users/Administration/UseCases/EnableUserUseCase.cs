using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Users.Administration.Dtos;
using BudgetCivil.Application.Users.Administration.Repositories;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Users.Administration.UseCases;

public sealed class EnableUserUseCase(
    IUserAdministrationRepository repository,
    ISecurityAuditService securityAuditService)
{
    public async Task<EnableUserResponse> ExecuteAsync(
        Guid actorUserId,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        var targetUser = await repository.GetUserForManagementAsync(targetUserId, cancellationToken);

        AdministrationAuthorizationRules.EnsureActorCanManageTarget(actor, targetUser);

        // Habilitar también libera un bloqueo por intentos fallidos para que el acceso sea efectivo.
        targetUser!.AccountEnabled = true;
        targetUser.AccountLocked = false;
        targetUser.FailedLoginAttempts = 0;
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
                ReasonCode: "USER_ENABLED"),
            cancellationToken);

        return new EnableUserResponse
        {
            UserId = targetUser.Id,
            AccountEnabled = targetUser.AccountEnabled
        };
    }
}
