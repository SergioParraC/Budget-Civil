using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Common.Auditing;

public sealed class SecurityAuditService(ISecurityAuditRepository repository) : ISecurityAuditService
{
    public Task WriteAsync(SecurityAuditWriteRequest request, CancellationToken cancellationToken = default)
    {
        return repository.AppendAsync(request, cancellationToken);
    }

    public Task WriteAuthorizationDeniedAsync(
        Guid? actorUserId,
        Guid? companyId,
        string permissionKey,
        string reasonCode,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var enrichedMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (metadata is not null)
        {
            foreach (var item in metadata)
            {
                enrichedMetadata[item.Key] = item.Value;
            }
        }

        enrichedMetadata.TryAdd("audit_category", "AUTHORIZATION_DENIED");
        enrichedMetadata.TryAdd("permission_key_checked", permissionKey);
        enrichedMetadata.TryAdd("reason_code", reasonCode);

        var request = new SecurityAuditWriteRequest(
            actorUserId,
            companyId,
            SecurityAuditEventType.AuthorizationDenied,
            SecurityAuditResult.Denied,
            permissionKey,
            TargetType: null,
            TargetId: null,
            reasonCode,
            enrichedMetadata);

        return repository.AppendAsync(request, cancellationToken);
    }
}
