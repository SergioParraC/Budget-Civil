using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Dtos;

public sealed class UpdateMyProfileRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public string? SecondLastName { get; init; }

    public string DocumentType { get; init; } = string.Empty;

    public string DocumentNumber { get; init; } = string.Empty;

    public DateOnly BirthDate { get; init; }

    public ProfileResidenceRequest Residence { get; init; } = new();

    public string Email { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string? ProfessionalCard { get; init; }

    public DateOnly? ProfessionalCardIssuedAt { get; init; }

    public string? ProfessionalCardFilePath { get; init; }
}

public sealed class ProfileResidenceRequest
{
    public Guid CountryId { get; init; }

    public Guid DepartmentId { get; init; }

    public Guid MunicipalityId { get; init; }
}
