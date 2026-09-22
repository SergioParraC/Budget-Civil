using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.Dtos;

public sealed class CreateCompanyResponse
{
    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }

    [JsonPropertyName("fecha_creacion")]
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class UpdateCompanyResponse
{
    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }

    [JsonPropertyName("fecha_edicion")]
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class DeactivateCompanyResponse
{
    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }
}
