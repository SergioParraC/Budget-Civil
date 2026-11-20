using BudgetCivil.Application.Resources.Repositories;

namespace BudgetCivil.Application.Resources.Commands.ChangeResourceQuotationStatus;

/// <summary>
/// Regla de cotización por insumo (`REQ-COTIZACION-02`, `BR-009`): solo los completos pasan a `COTIZADO`.
/// La Matriz se evalúa por unidad y componentes, porque su precio se deriva de la composición
/// (`REQ-APUS-06`, `SC-COTIZACION-07`).
/// </summary>
public static class ResourceQuotationRules
{
    public const string IncompleteCode = "RESOURCE_QUOTATION_INCOMPLETE";

    public static bool CanQuote(ResourceQuotationCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return candidate.IsMatrix
            ? !string.IsNullOrWhiteSpace(candidate.Unit) && candidate.MatrixComponentCount > 0
            : candidate.IsComplete;
    }
}
