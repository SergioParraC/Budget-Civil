using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Profile.Dtos;

public sealed class UpdateMyProfileRequest
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
    public ProfileResidenceRequest Residence { get; init; } = new();

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("tarjeta_profesional")]
    public string? ProfessionalCard { get; init; }

    [JsonPropertyName("fecha_expedicion_tarjeta_profesional")]
    public DateOnly? ProfessionalCardIssuedAt { get; init; }

    [JsonPropertyName("archivo_tarjeta_profesional")]
    public string? ProfessionalCardFilePath { get; init; }
}

public sealed class ProfileResidenceRequest
{
    [JsonPropertyName("municipio")]
    public string City { get; init; } = string.Empty;

    [JsonPropertyName("departamento")]
    public string Department { get; init; } = string.Empty;

    [JsonPropertyName("pais")]
    public string Country { get; init; } = string.Empty;
}
