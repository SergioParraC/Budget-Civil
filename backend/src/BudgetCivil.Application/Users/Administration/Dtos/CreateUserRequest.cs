using System.Text.Json;
using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Administration.Dtos;

public sealed class CreateUserRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public string? SecondLastName { get; init; }

    public string DocumentType { get; init; } = string.Empty;

    public string DocumentNumber { get; init; } = string.Empty;

    public DateOnly BirthDate { get; init; }

    public UserResidenceRequest Residence { get; init; } = new();

    public string Email { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string? AssignedRoleCode { get; init; }

    public string? ProfessionalCard { get; init; }

    public DateOnly? ProfessionalCardIssuedAt { get; init; }

    public string? ProfessionalCardFilePath { get; init; }

    public Guid? CompanyId { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; init; }
}
