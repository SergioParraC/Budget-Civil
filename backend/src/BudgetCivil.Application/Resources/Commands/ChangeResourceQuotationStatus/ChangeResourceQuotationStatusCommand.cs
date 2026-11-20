using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Application.Resources.Commands.ChangeResourceQuotationStatus;

public sealed class ResourceQuotationFilterCommand
{
    public string? Description { get; init; }

    public Guid? ResourceTypeId { get; init; }

    public string? QuotationStatus { get; init; }

    public string? Brand { get; init; }

    public Guid? MunicipalityId { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }
}

/// <summary>
/// Cambio masivo de insumos a `COTIZADO` con `scope` discriminado (`SELECTION`, `FILTER`, `ALL`) y sin
/// `rowVersion`: el lote se evalúa en el servidor (`D-11`, `D-12`, `REQ-COTIZACION-01`…`05`).
/// </summary>
public sealed class ChangeResourceQuotationStatusCommand
{
    public string? Scope { get; init; }

    public IReadOnlyList<Guid>? ResourceIds { get; init; }

    public ResourceQuotationFilterCommand? Filter { get; init; }

    public static bool TryParseScope(string? value, out ResourceQuotationScope scope)
    {
        switch (value?.Trim().ToUpperInvariant())
        {
            case "SELECTION":
                scope = ResourceQuotationScope.Selection;
                return true;
            case "FILTER":
                scope = ResourceQuotationScope.Filter;
                return true;
            case "ALL":
                scope = ResourceQuotationScope.All;
                return true;
            default:
                scope = ResourceQuotationScope.Selection;
                return false;
        }
    }
}

public sealed class ResourceQuotationSkippedDto
{
    public Guid ResourceId { get; init; }

    public string Code { get; init; } = string.Empty;
}

public sealed class ChangeResourceQuotationStatusResponse
{
    public Guid ProjectId { get; init; }

    public string QuotationStatus { get; init; } = QuotationStatusCodes.Cotizado;

    public int Requested { get; init; }

    public int Updated { get; init; }

    public int AlreadyQuoted { get; init; }

    public IReadOnlyList<ResourceQuotationSkippedDto> Skipped { get; init; } = [];

    public int PendingResources { get; init; }

    public bool AllQuoted { get; init; }
}
