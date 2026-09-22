using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Bootstrap.Dtos;

public sealed class BootstrapResponse
{
    [JsonPropertyName("bootstrap_enabled")]
    public bool BootstrapEnabled { get; init; }

    [JsonPropertyName("administrator_created")]
    public bool AdministratorCreated { get; init; }

    [JsonPropertyName("administrator_id")]
    public Guid AdministratorId { get; init; }

    [JsonPropertyName("audit_event_id")]
    public Guid AuditEventId { get; init; }
}

public sealed class BootstrapAuditRecordResponse
{
    [JsonPropertyName("actor")]
    public string Actor { get; init; } = string.Empty;

    [JsonPropertyName("accion")]
    public string Action { get; init; } = string.Empty;

    [JsonPropertyName("objetivo")]
    public string Target { get; init; } = string.Empty;

    [JsonPropertyName("fecha_hora")]
    public DateTime OccurredAtUtc { get; init; }

    [JsonPropertyName("resultado")]
    public string Result { get; init; } = string.Empty;
}
