using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.Dtos;

public sealed class UpdateCompanyRequest
{
    public string BusinessName { get; init; } = string.Empty;

    public string TaxId { get; init; } = string.Empty;

    public string CompanyType { get; init; } = string.Empty;

    public string? TerritorialLevel { get; init; }

    public Guid CountryId { get; init; }

    public Guid DepartmentId { get; init; }

    public Guid MunicipalityId { get; init; }

    public LegalRepresentativePayload? Representative { get; init; }

    public bool? IsActive { get; init; }
}
