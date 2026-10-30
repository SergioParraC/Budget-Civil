using System.Text.Json.Serialization;
using BudgetCivil.Application.Companies.Dtos;

namespace BudgetCivil.Application.Users.Bootstrap.Dtos;

public sealed class BootstrapRequest
{
    public BootstrapAdminProfileRequest AdminProfile { get; init; } = new();

    public BootstrapCompanyRequest Company { get; init; } = new();
}

public sealed class BootstrapAdminProfileRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string DocumentType { get; init; } = string.Empty;

    public string DocumentNumber { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public Guid CountryId { get; init; }

    public Guid DepartmentId { get; init; }

    public Guid MunicipalityId { get; init; }
}

public sealed class BootstrapCompanyRequest
{
    public string BusinessName { get; init; } = string.Empty;

    public string TaxId { get; init; } = string.Empty;

    public string CompanyType { get; init; } = string.Empty;

    public string? TerritorialLevel { get; init; }

    public Guid CountryId { get; init; }

    public Guid DepartmentId { get; init; }

    public Guid MunicipalityId { get; init; }

    public LegalRepresentativePayload? Representative { get; init; }
}
