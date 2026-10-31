using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Dtos;

public sealed class GetMyProfileResponse
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public Guid? CompanyId { get; init; }

    public string RoleCode { get; init; } = string.Empty;

    public bool AccountEnabled { get; init; }

    public ProfileSummaryResponse Profile { get; init; } = new();
}

public sealed class ProfileSummaryResponse
{
    public string FirstName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public string? SecondLastName { get; init; }

    public string DocumentType { get; init; } = string.Empty;

    public string DocumentNumber { get; init; } = string.Empty;

    public DateOnly BirthDate { get; init; }

    public ProfileResidenceResponse Residence { get; init; } = new();

    public string? ProfessionalCard { get; init; }

    public DateOnly? ProfessionalCardIssuedAt { get; init; }

    public string? ProfessionalCardFilePath { get; init; }
}

public sealed class ProfileResidenceResponse
{
    public string? City { get; init; }

    public string? Department { get; init; }

    public string? Country { get; init; }

    public Guid? CountryId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? MunicipalityId { get; init; }
}

public sealed class UpdateMyProfileResponse
{
    public Guid UserId { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class ChangeMyPasswordResponse
{
    public bool PasswordUpdated { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class EnableUserPasswordRecoveryResponse
{
    public Guid UserId { get; init; }

    public bool RecoveryEnabled { get; init; }
}
