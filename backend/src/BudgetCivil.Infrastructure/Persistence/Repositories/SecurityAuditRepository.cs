using System.Text.Json;
using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class SecurityAuditRepository(BudgetCivilDbContext dbContext) : ISecurityAuditRepository
{
    public async Task AppendAsync(SecurityAuditWriteRequest request, CancellationToken cancellationToken = default)
    {
        var auditEvent = new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = request.ActorUserId,
            CompanyId = request.CompanyId,
            EventType = request.EventType,
            Result = request.Result,
            PermissionKey = request.PermissionKey,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            ReasonCode = request.ReasonCode,
            OccurredAtUtc = DateTime.UtcNow,
            MetadataJson = request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata)
        };

        dbContext.SecurityAuditEvents.Add(auditEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
