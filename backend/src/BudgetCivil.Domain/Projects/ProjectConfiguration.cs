namespace BudgetCivil.Domain.Projects;

/// <summary>
/// Configuración del proyecto en entidad propia y 1:1 con <see cref="Project"/> (`D-93`). Concentra la
/// divisa base, el tope presupuestal, los tres componentes del AIU, el modo de firma, el firmante y el
/// requisito de auditor. El proyecto tiene exactamente una configuración: el identificador es el mismo
/// del proyecto. La verificación y las alertas del tope (`RF-17`) pertenecen a `E-4a` (`D-94`).
/// </summary>
public sealed class ProjectConfiguration
{
    public Guid ProjectId { get; set; }

    public Guid BaseCurrencyId { get; set; }

    public decimal? BudgetLimit { get; set; }

    public decimal? AiuAdminPercent { get; set; }

    public decimal? AiuUnforeseenPercent { get; set; }

    public decimal? AiuProfitPercent { get; set; }

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
}
