using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Profile.Dtos;
using BudgetCivil.Application.Profile.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Profile.UseCases;

public sealed class ChangeMyPasswordUseCase(
    IProfileRepository repository,
    IPasswordHasher passwordHasher,
    ISecurityAuditService securityAuditService,
    IValidator<ChangeMyPasswordRequest> validator)
{
    public async Task<ChangeMyPasswordResponse> ExecuteAsync(
        Guid actorUserId,
        ChangeMyPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken);
        if (actor is null)
        {
            throw new ForbiddenAppException("Authenticated user does not exist.", "AUTH_USER_NOT_FOUND");
        }

        if (!passwordHasher.VerifyHashedPassword(actor.PasswordHash, request.CurrentPassword))
        {
            throw new UnauthorizedAppException("Current password is invalid.", "INVALID_CURRENT_PASSWORD");
        }

        var utcNow = DateTime.UtcNow;

        actor.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        actor.FailedLoginAttempts = 0;
        actor.UpdatedAtUtc = utcNow;

        await repository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                actor.CompanyId,
                SecurityAuditEventType.PasswordRecovery,
                SecurityAuditResult.Success,
                PermissionKey: "PROFILE_CHANGE_PASSWORD_SELF",
                TargetType: nameof(UserAccount),
                TargetId: actor.Id,
                ReasonCode: "PROFILE_PASSWORD_CHANGED"),
            cancellationToken);

        return new ChangeMyPasswordResponse
        {
            PasswordUpdated = true,
            UpdatedAtUtc = utcNow
        };
    }

    private async Task ValidateAsync(ChangeMyPasswordRequest request, CancellationToken cancellationToken)
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
