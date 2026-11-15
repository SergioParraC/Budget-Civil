using BudgetCivil.Application.Wbs.Dtos;

namespace BudgetCivil.Application.Apus.Dtos;

/// <summary>
/// Respuesta plana de la creación, edición y copia unificadas de ítem EDT + APU (`REQ-APUS-21`,
/// `REQ-EDT-22`, `D5`). Se usa `rowVersion` para el ítem y `apuRowVersion` para el APU.
/// </summary>
public sealed class WbsItemApuResponse
{
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    public string NodeType { get; init; } = string.Empty;

    public string Code { get; init; } = string.Empty;

    public Guid? ParentChapterId { get; init; }

    public string? Name { get; init; }

    public decimal QuantityWork { get; init; }

    public int OrderIndex { get; init; }

    public string RowVersion { get; init; } = string.Empty;

    public Guid LastModifiedBy { get; init; }

    public DateTime LastModifiedAt { get; init; }

    public IReadOnlyList<WbsNodeDto> Children { get; init; } = [];

    public Guid ApuId { get; init; }

    public string ApuStatus { get; init; } = string.Empty;

    public string ApuRowVersion { get; init; } = string.Empty;

    public Guid ApuLastModifiedBy { get; init; }

    public DateTime ApuLastModifiedAt { get; init; }

    public string Description { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public IReadOnlyList<ApuResourceGroupDto> Resources { get; init; } = [];

    public decimal? DirectCost { get; init; }

    public IndirectCostsDto IndirectCosts { get; init; } = new();

    public decimal? Total { get; init; }
}

public static class WbsItemApuMapper
{
    public static WbsItemApuResponse ToResponse(WbsNodeDto item, ApuDetailResponse apu)
    {
        return new WbsItemApuResponse
        {
            Id = item.Id,
            ProjectId = item.ProjectId,
            NodeType = item.NodeType,
            Code = item.Code,
            ParentChapterId = item.ParentChapterId,
            Name = item.Name,
            QuantityWork = item.QuantityWork ?? 0m,
            OrderIndex = item.OrderIndex,
            RowVersion = item.RowVersion,
            LastModifiedBy = item.LastModifiedBy,
            LastModifiedAt = item.LastModifiedAt,
            Children = item.Children,
            ApuId = apu.ApuId,
            ApuStatus = apu.Status,
            ApuRowVersion = apu.RowVersion,
            ApuLastModifiedBy = apu.LastModifiedBy,
            ApuLastModifiedAt = apu.LastModifiedAt,
            Description = apu.Description,
            Unit = apu.Unit,
            Resources = apu.Resources,
            DirectCost = apu.DirectCost,
            IndirectCosts = apu.IndirectCosts,
            Total = apu.Total
        };
    }
}
