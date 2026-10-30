using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Bootstrap.Dtos;

public sealed class BootstrapResponse
{
    public bool BootstrapEnabled { get; init; }

    public bool AdministratorCreated { get; init; }

    public Guid AdministratorId { get; init; }

    public Guid AuditEventId { get; init; }
}

public sealed class BootstrapAuditRecordResponse
{
    public string Actor { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string Target { get; init; } = string.Empty;

    public DateTime OccurredAtUtc { get; init; }

    public string Result { get; init; } = string.Empty;
}
