using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Administration.Dtos;

public sealed class ChangeUserCompanyRequest
{
    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; init; }
}
