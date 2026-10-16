using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Profile.Dtos;
using BudgetCivil.Application.Profile.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Profile.UseCases;

public sealed class EnableUserPasswordRecoveryUseCase(
    IProfileRepository repository,
    ISecurityAuditService securityAuditService,
    IValidator<EnableUserPasswordRecoveryRequest> validator)
{
    public async Task<EnableUserPasswordRecoveryResponse> ExecuteAsync(
        Guid actorUserId,
        Guid targetUserId,
        EnableUserPasswordRecoveryRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            throw new ForbiddenAppException("Authenticated user does not exist.", "AUTH_USER_NOT_FOUND");
        }

        if (actor.Role.Code != "ADMINISTRADOR")
        {
            throw new ForbiddenAppException("Only Administrador can enable password recovery.", "ROLE_NOT_ALLOWED");
        }

        var targetUser = await repository.GetActorByIdAsync(targetUserId, cancellationToken);
        if (targetUser is null)
        {
            throw new ConflictAppException("Target user was not found.", "TARGET_USER_NOT_FOUND");
        }

        var activeGrants = await repository.GetActiveRecoveryGrantsAsync(targetUserId, cancellationToken);
        foreach (var grant in activeGrants)
        {
            grant.IsActive = false;
        }

        await repository.AddRecoveryGrantAsync(
            new PasswordRecoveryGrant
            {
                Id = Guid.NewGuid(),
                UserId = targetUserId,
                EnabledByUserId = actor.Id,
                Reason = request.Reason.Trim(),
                EnabledAtUtc = DateTime.UtcNow,
                IsActive = true,
                ConsumedAtUtc = null,
                ExpiresAtUtc = null
            },
            cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                actor.CompanyId,
                SecurityAuditEventType.PasswordRecovery,
                SecurityAuditResult.Success,
                PermissionKey: "USERS_PASSWORD_RECOVERY_ENABLE",
                TargetType: nameof(UserAccount),
                TargetId: targetUserId,
                ReasonCode: "PASSWORD_RECOVERY_ENABLED"),
            cancellationToken);

        return new EnableUserPasswordRecoveryResponse
        {
            UserId = targetUserId,
            RecoveryEnabled = true
        };
    }

    private async Task ValidateAsync(EnableUserPasswordRecoveryRequest request, CancellationToken cancellationToken)
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
