using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Common.Auditing;

public sealed record SecurityAuditWriteRequest(
    Guid? ActorUserId,
    Guid? CompanyId,
    SecurityAuditEventType EventType,
    SecurityAuditResult Result,
    string? PermissionKey = null,
    string? TargetType = null,
    Guid? TargetId = null,
    string? ReasonCode = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
