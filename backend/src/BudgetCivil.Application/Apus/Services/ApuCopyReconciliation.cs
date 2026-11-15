using BudgetCivil.Application.Apus.Repositories;

namespace BudgetCivil.Application.Apus.Services;

public sealed record ResourceMatchResult(
    string Match,
    IReadOnlyList<SimpleResourceSnapshot> Candidates);

/// <summary>
/// Comparativo entre el insumo del proyecto de origen y el candidato del proyecto destino. La unidad y el
/// precio unitario se informan por separado para que el cliente decida con datos completos antes de
/// enviar una resolución (`REQ-APUS-10`, `D9`). `UnitPriceDifference` es `candidato − origen` y solo se
/// informa cuando ambos precios existen.
/// </summary>
public sealed record ResourceComparison(
    bool UnitMatches,
    bool UnitPriceMatches,
    decimal? UnitPriceDifference);

/// <summary>
/// Clasificación de insumos simples para la copia de un APU (`REQ-APUS-10`, `D9`, `D-25`). Solo compara
/// insumos simples: las Matrices se copian siempre sin validación.
/// `EXACT` significa que la descripción normalizada coincide; la unidad y el precio unitario se comparan
/// aparte con <see cref="Compare"/> y no condicionan la clasificación.
/// </summary>
public static class ApuCopyReconciliation
{
    public const string Exact = "EXACT";
    public const string Similar = "SIMILAR";
    public const string None = "NONE";
    public const int MaxCandidates = 10;

    public static ResourceMatchResult Classify(
        SimpleResourceSnapshot source,
        IReadOnlyList<SimpleResourceSnapshot> targetResources)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(targetResources);

        var sourceDescription = Normalize(source.Description);

        // `EXACT` compara el nombre del insumo, no la unidad ni el precio: un material con la misma
        // descripción pero distinta unidad o precio sigue siendo un conflicto que el usuario debe resolver
        // viendo el comparativo (`SC-APUS-48`).
        var exact = targetResources
            .Where(resource => Normalize(resource.Description) == sourceDescription)
            .Take(MaxCandidates)
            .ToList();

        if (exact.Count > 0)
        {
            return new ResourceMatchResult(Exact, exact);
        }

        var similar = targetResources
            .Where(resource =>
            {
                var candidate = Normalize(resource.Description);
                return candidate.Length > 0 &&
                       sourceDescription.Length > 0 &&
                       (candidate.Contains(sourceDescription, StringComparison.Ordinal) ||
                        sourceDescription.Contains(candidate, StringComparison.Ordinal));
            })
            .Take(MaxCandidates)
            .ToList();

        return similar.Count > 0
            ? new ResourceMatchResult(Similar, similar)
            : new ResourceMatchResult(None, []);
    }

    /// <summary>
    /// Compara unidad y precio unitario del insumo de origen con un candidato del destino. Se usa tanto
    /// para `EXACT` como para `SIMILAR`; en `NONE` no hay candidato que comparar.
    /// </summary>
    public static ResourceComparison Compare(
        string? sourceUnit,
        decimal? sourceUnitPrice,
        string? candidateUnit,
        decimal? candidateUnitPrice)
    {
        var unitMatches = Normalize(sourceUnit) == Normalize(candidateUnit);
        var unitPriceMatches = sourceUnitPrice == candidateUnitPrice;

        var unitPriceDifference = sourceUnitPrice.HasValue && candidateUnitPrice.HasValue
            ? candidateUnitPrice.Value - sourceUnitPrice.Value
            : (decimal?)null;

        return new ResourceComparison(unitMatches, unitPriceMatches, unitPriceDifference);
    }

    private static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}

