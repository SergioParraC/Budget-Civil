namespace BudgetCivil.Domain.Projects;

public sealed class Project
{
    public const int NameMaxLength = 255;
    public const int CodeMaxLength = 50;
    public const int DescriptionMaxLength = 1000;
    public const int ObjectiveMaxLength = 1000;

    // D-03/D-90: tres modos de firma. El modo sin firma y los dos modos con firma del presupuestador.
    public const string NoSignature = "SIN_FIRMA";
    public const string UniqueSignerByAssignedEstimator = "UNICA_POR_PRESUPUESTADOR_ASIGNADO";
    public const string IndependentSignerByResponsibleProfessional = "INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE";

    public const string DefaultSignatureMode = UniqueSignerByAssignedEstimator;

    public static readonly IReadOnlyList<string> SignatureModes =
    [
        NoSignature,
        UniqueSignerByAssignedEstimator,
        IndependentSignerByResponsibleProfessional
    ];

    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string? Code { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Objective { get; set; }

    public Guid CountryId { get; set; }

    public Guid DepartmentId { get; set; }

    public Guid MunicipalityId { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.PorCotizar;

    public bool IsPublic { get; set; }

    public Guid DirectorOwnerUserId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public Guid? LastModifiedByUserId { get; set; }

    public DateTime? LastModifiedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ProjectConfiguration Configuration { get; set; } = null!;

    public static string NormalizeName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > NameMaxLength)
        {
            throw new InvalidOperationException("Project name is invalid.");
        }

        return normalized;
    }

    public static string? NormalizeCode(string? code)
    {
        return NormalizeOptionalText(code, CodeMaxLength, "code");
    }

    public static string? NormalizeDescription(string? description)
    {
        return NormalizeOptionalText(description, DescriptionMaxLength, "description");
    }

    public static string? NormalizeObjective(string? objective)
    {
        return NormalizeOptionalText(objective, ObjectiveMaxLength, "objective");
    }

    public static bool IsValidSignatureMode(string? signatureMode)
    {
        if (string.IsNullOrWhiteSpace(signatureMode))
        {
            return false;
        }

        var normalized = signatureMode.Trim().ToUpperInvariant();
        return SignatureModes.Contains(normalized, StringComparer.Ordinal);
    }

    public static string NormalizeSignatureModeOrDefault(string? signatureMode)
    {
        if (string.IsNullOrWhiteSpace(signatureMode))
        {
            return DefaultSignatureMode;
        }

        var normalized = signatureMode.Trim().ToUpperInvariant();
        if (!IsValidSignatureMode(normalized))
        {
            throw new InvalidOperationException("Project signature mode is invalid.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new InvalidOperationException($"Project {fieldName} is invalid.");
        }

        return normalized;
    }
}
