namespace BudgetCivil.Domain.Projects;

public sealed class Project
{
    public const int NameMaxLength = 255;
    public const int DescriptionMaxLength = 1000;
    public const int ObjectiveMaxLength = 1000;

    public const string UniqueSignerByAssignedEstimator = "UNICA_POR_PRESUPUESTADOR_ASIGNADO";
    public const string IndependentSignerByResponsibleProfessional = "INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE";

    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Objective { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.PorCotizar;

    public bool IsPublic { get; set; }

    public string SigningMode { get; set; } = "UNICA_POR_PRESUPUESTADOR_ASIGNADO";

    public Guid? SignerUserId { get; set; }

    public Guid DirectorOwnerUserId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public static string NormalizeName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > NameMaxLength)
        {
            throw new InvalidOperationException("Project name is invalid.");
        }

        return normalized;
    }

    public static string? NormalizeDescription(string? description)
    {
        return NormalizeOptionalText(description, DescriptionMaxLength, "description");
    }

    public static string? NormalizeObjective(string? objective)
    {
        return NormalizeOptionalText(objective, ObjectiveMaxLength, "objective");
    }

    public static bool IsValidSigningMode(string? signingMode)
    {
        if (string.IsNullOrWhiteSpace(signingMode))
        {
            return false;
        }

        var normalized = signingMode.Trim().ToUpperInvariant();
        return normalized is UniqueSignerByAssignedEstimator or IndependentSignerByResponsibleProfessional;
    }

    public static string NormalizeSigningModeOrDefault(string? signingMode)
    {
        if (string.IsNullOrWhiteSpace(signingMode))
        {
            return UniqueSignerByAssignedEstimator;
        }

        var normalized = signingMode.Trim().ToUpperInvariant();
        if (!IsValidSigningMode(normalized))
        {
            throw new InvalidOperationException("Project signing mode is invalid.");
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
