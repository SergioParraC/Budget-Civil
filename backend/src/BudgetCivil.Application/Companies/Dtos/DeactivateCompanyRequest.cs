using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.Dtos;

public sealed class DeactivateCompanyRequest
{
    public string Reason { get; init; } = string.Empty;
}
