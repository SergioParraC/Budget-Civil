using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.Dtos;

public sealed class LegalRepresentativePayload
{
    [JsonPropertyName("mode")]
    public string? Mode { get; init; }

    [JsonPropertyName("user_id")]
    public Guid? UserId { get; init; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; init; }

    [JsonPropertyName("document_number")]
    public string? DocumentNumber { get; init; }

    [JsonPropertyName("document_type_id")]
    public Guid? DocumentTypeId { get; init; }
}
