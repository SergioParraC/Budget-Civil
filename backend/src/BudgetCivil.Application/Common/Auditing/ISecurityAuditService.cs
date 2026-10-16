using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Common.Auditing;

public interface ISecurityAuditService
{
    Task WriteAsync(SecurityAuditWriteRequest request, CancellationToken cancellationToken = default);

    Task WriteAuthorizationDeniedAsync(
        Guid? actorUserId,
        Guid? companyId,
        string permissionKey,
        string reasonCode,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}
