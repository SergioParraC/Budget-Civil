using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Companies.ChangeCompanyStatus;

public sealed class EnableCompanyCommand
{
    public string? Reason { get; init; }
}
