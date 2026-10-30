using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.Dtos;

public sealed class CreateCompanyResponse
{
    public Guid CompanyId { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class UpdateCompanyResponse
{
    public Guid CompanyId { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class DeactivateCompanyResponse
{
    public Guid CompanyId { get; init; }

    public bool IsActive { get; init; }
}
