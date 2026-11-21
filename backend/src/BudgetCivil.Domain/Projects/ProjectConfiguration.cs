namespace BudgetCivil.Domain.Projects;

/// <summary>
/// Configuración del proyecto en entidad propia y 1:1 con <see cref="Project"/> (`D-93`). Concentra la
/// divisa base, el tope presupuestal, los tres componentes del AIU, el modo de firma, el firmante y el
/// requisito de auditor. El proyecto tiene exactamente una configuración: el identificador es el mismo
/// del proyecto. La verificación y las alertas del tope (`RF-17`) pertenecen a `E-4a` (`D-94`).
/// </summary>
public sealed class ProjectConfiguration
{
    public const int DefaultDecimals = 7;
    public const int MinDecimals = 0;
    public const int MaxDecimals = 10;

    public Guid ProjectId { get; set; }

    public Guid BaseCurrencyId { get; set; }

    public decimal? BudgetLimit { get; set; }

    public decimal? AiuAdminPercent { get; set; }

    public decimal? AiuUnforeseenPercent { get; set; }

    public decimal? AiuProfitPercent { get; set; }

    /// <summary>Decimales de redondeo de todos los cálculos (`REQ-PROYECTOS-10`, `D-121`).</summary>
    public int RoundingDecimals { get; set; } = DefaultDecimals;

    /// <summary>Decimales de las cantidades efectivas de la vista previa de costo (`D-121`).</summary>
    public int QuantityDecimals { get; set; } = DefaultDecimals;

    public string SignatureMode { get; set; } = Project.DefaultSignatureMode;

    public Guid? SignerUserId { get; set; }

    public bool RequiresAuditor { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public static decimal? NormalizeBudgetLimit(decimal? budgetLimit)
    {
        if (!budgetLimit.HasValue)
        {
            return null;
        }

        if (budgetLimit.Value <= 0m)
        {
            throw new InvalidOperationException("Project budget limit is invalid.");
        }

        return decimal.Round(budgetLimit.Value, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal? NormalizeAiuPercent(decimal? aiuPercent, string fieldName)
    {
        if (!aiuPercent.HasValue)
        {
            return null;
        }

        if (aiuPercent.Value is < 0m or > 100m)
        {
            throw new InvalidOperationException($"Project {fieldName} is invalid.");
        }

        return decimal.Round(aiuPercent.Value, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Normaliza los decimales de redondeo y cantidades al rango 0–10 y aplica 7 por defecto
    /// (`REQ-PROYECTOS-10`, `D-121`).
    /// </summary>
    public static int NormalizeDecimals(int? decimals, int defaultValue = DefaultDecimals)
    {
        var resolved = decimals ?? defaultValue;
        if (resolved is < MinDecimals or > MaxDecimals)
        {
            throw new InvalidOperationException("Project decimals are invalid.");
        }

        return resolved;
    }
}
