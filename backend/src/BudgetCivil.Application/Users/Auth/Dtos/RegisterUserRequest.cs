using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Auth.Dtos;

public sealed class RegisterUserRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public string? SecondLastName { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string DocumentType { get; init; } = string.Empty;

    public string DocumentNumber { get; init; } = string.Empty;

    public DateOnly BirthDate { get; init; }

    public Guid CountryId { get; init; }

    public Guid DepartmentId { get; init; }

    public Guid MunicipalityId { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; init; }
}
