using System.Text.Json.Serialization;
using BudgetCivil.Application.Companies.Dtos;

namespace BudgetCivil.Application.Users.Bootstrap.Dtos;

public sealed class BootstrapRequest
{
    [JsonPropertyName("admin_profile")]
    public BootstrapAdminProfileRequest AdminProfile { get; init; } = new();

    [JsonPropertyName("company")]
    public BootstrapCompanyRequest Company { get; init; } = new();
}

public sealed class BootstrapAdminProfileRequest
{
    [JsonPropertyName("primer_nombre")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("primer_apellido")]
    public string LastName { get; init; } = string.Empty;

    [JsonPropertyName("tipo_documento")]
    public string DocumentType { get; init; } = string.Empty;

    [JsonPropertyName("numero_documento")]
    public string DocumentNumber { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;

    [JsonPropertyName("country_id")]
    public Guid CountryId { get; init; }

    [JsonPropertyName("department_id")]
    public Guid DepartmentId { get; init; }

    [JsonPropertyName("municipality_id")]
    public Guid MunicipalityId { get; init; }
}

public sealed class BootstrapCompanyRequest
{
    [JsonPropertyName("business_name")]
    public string BusinessName { get; init; } = string.Empty;

    [JsonPropertyName("tax_id")]
    public string TaxId { get; init; } = string.Empty;

    [JsonPropertyName("company_type")]
    public string CompanyType { get; init; } = string.Empty;

    [JsonPropertyName("territorial_level")]
    public string? TerritorialLevel { get; init; }

    [JsonPropertyName("country_id")]
    public Guid CountryId { get; init; }

    [JsonPropertyName("department_id")]
    public Guid DepartmentId { get; init; }

    [JsonPropertyName("municipality_id")]
    public Guid MunicipalityId { get; init; }

    [JsonPropertyName("representative")]
    public LegalRepresentativePayload? Representative { get; init; }
}
