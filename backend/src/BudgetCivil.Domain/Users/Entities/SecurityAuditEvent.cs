namespace BudgetCivil.Domain.Users.Entities;

public enum SecurityAuditEventType
{
    AuthLogin = 1,
    AuthLockout = 2,
    AuthorizationDenied = 3,
    Bootstrap = 4,
    UserAdministration = 5,
    CompanyAdministration = 6,
    PasswordRecovery = 7
}

public enum SecurityAuditResult
{
    Success = 1,
    Denied = 2,
    Failed = 3
}

public sealed class SecurityAuditEvent
{
    public Guid Id { get; set; }

    public Guid? ActorUserId { get; set; }

    public Guid? CompanyId { get; set; }

    public SecurityAuditEventType EventType { get; set; }

    public string? PermissionKey { get; set; }

    public string? TargetType { get; set; }

    public Guid? TargetId { get; set; }

    public string? ReasonCode { get; set; }

    public SecurityAuditResult Result { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public string? MetadataJson { get; set; }

    public UserAccount? ActorUser { get; set; }
}
