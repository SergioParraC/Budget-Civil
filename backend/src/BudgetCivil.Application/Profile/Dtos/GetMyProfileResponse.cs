using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Profile.Dtos;

public sealed class GetMyProfileResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("company_id")]
    public Guid? CompanyId { get; init; }

    [JsonPropertyName("role_code")]
    public string RoleCode { get; init; } = string.Empty;

    [JsonPropertyName("estado_cuenta")]
    public bool AccountEnabled { get; init; }

    [JsonPropertyName("perfil")]
    public ProfileSummaryResponse Profile { get; init; } = new();
}

public sealed class ProfileSummaryResponse
{
    [JsonPropertyName("primer_nombre")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("segundo_nombre")]
    public string? MiddleName { get; init; }

    [JsonPropertyName("primer_apellido")]
    public string LastName { get; init; } = string.Empty;

    [JsonPropertyName("segundo_apellido")]
    public string? SecondLastName { get; init; }

    [JsonPropertyName("tipo_documento")]
    public string DocumentType { get; init; } = string.Empty;

    [JsonPropertyName("numero_documento")]
    public string DocumentNumber { get; init; } = string.Empty;

    [JsonPropertyName("fecha_nacimiento")]
    public DateOnly BirthDate { get; init; }

    [JsonPropertyName("residencia")]
    public ProfileResidenceResponse Residence { get; init; } = new();

    [JsonPropertyName("tarjeta_profesional")]
    public string? ProfessionalCard { get; init; }

    [JsonPropertyName("fecha_expedicion_tarjeta_profesional")]
    public DateOnly? ProfessionalCardIssuedAt { get; init; }

    [JsonPropertyName("archivo_tarjeta_profesional")]
    public string? ProfessionalCardFilePath { get; init; }
}

public sealed class ProfileResidenceResponse
{
    [JsonPropertyName("municipio")]
    public string? City { get; init; }

    [JsonPropertyName("departamento")]
    public string? Department { get; init; }

    [JsonPropertyName("pais")]
    public string? Country { get; init; }

    [JsonPropertyName("country_id")]
    public Guid? CountryId { get; init; }

    [JsonPropertyName("department_id")]
    public Guid? DepartmentId { get; init; }

    [JsonPropertyName("municipality_id")]
    public Guid? MunicipalityId { get; init; }
}

public sealed class UpdateMyProfileResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("fecha_edicion")]
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class ChangeMyPasswordResponse
{
    [JsonPropertyName("password_updated")]
    public bool PasswordUpdated { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class EnableUserPasswordRecoveryResponse
{
    [JsonPropertyName("user_id")]
    public Guid UserId { get; init; }

    [JsonPropertyName("recovery_enabled")]
    public bool RecoveryEnabled { get; init; }
}
