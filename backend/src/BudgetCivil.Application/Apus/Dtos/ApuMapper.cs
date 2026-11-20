using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Calculation;
using BudgetCivil.Domain.Apus;

namespace BudgetCivil.Application.Apus.Dtos;

/// <summary>Mapeo manual, sin AutoMapper ni Mapster (`REQ-APUS-19`, `T-C-05`).</summary>
public static class ApuMapper
{
    public static ApuDetailResponse ToDetail(
        Apu apu,
        ApuCostPayload payload,
        ApuStatus status,
        WbsItemInfo? wbsItem,
        bool includeQuantityWork)
    {
        return new ApuDetailResponse
        {
            ApuId = apu.Id,
            Description = apu.Description,
            Unit = apu.Unit,
            Status = ApuStatusCodes.ToCode(status),
            WbsItem = wbsItem is null
                ? null
                : new WbsItemReferenceDto
                {
                    Code = wbsItem.Code,
                    QuantityWork = includeQuantityWork ? wbsItem.QuantityWork : null,
                    Unit = apu.Unit
                },
            Resources = ToResourceGroups(payload.Lines),
            RowVersion = Convert.ToBase64String(apu.RowVersion),
            LastModifiedBy = apu.LastModifiedByUserId,
            LastModifiedAt = apu.LastModifiedAtUtc
        };
    }

    public static ApuListItemDto ToListItem(ApuListRow row, ApuCostResult cost)
    {
        return new ApuListItemDto
        {
            ApuId = row.ApuId,
            Code = row.ItemCode,
            Description = row.Description,
            Unit = row.Unit,
            UnitPrice = cost.DirectCost,
            ProjectId = row.ProjectId,
            ProjectName = row.ProjectName,
            MunicipalityName = row.MunicipalityName,
            DepartmentName = row.DepartmentName
        };
    }

    public static MatrixDetailResponse ToMatrixDetail(
        MatrixStateSnapshot state,
        MatrixCostResult cost,
        IReadOnlyList<MatrixComponentRow> components)
    {
        return new MatrixDetailResponse
        {
            MatrixResourceId = state.MatrixResourceId,
            Description = state.Description,
            Unit = state.Unit,
            Status = cost.IsComplete ? "POR_COTIZAR" : "FALTA_INFORMACION",
            Resources = ToMatrixResourceGroups(cost.Resources, components),
            DirectCost = cost.DirectCost,
            RowVersion = Convert.ToBase64String(state.RowVersion)
        };
    }

    private static readonly string[] ResourceTypeOrder =
    [
        "MATERIAL",
        "LABOR",
        "TOOL",
        "EQUIPMENT",
        "AUXILIARY",
        "WORK",
        "TRANSPORT",
        "MATRIX"
    ];

    public static IReadOnlyList<ApuResourceGroupDto> ToResourceGroups(
        IReadOnlyList<ApuCostLineSource> lines)
    {
        return lines
            .GroupBy(line => string.IsNullOrWhiteSpace(line.ResourceTypeCode)
                ? string.Empty
                : line.ResourceTypeCode.Trim().ToUpperInvariant())
            .OrderBy(group => Array.IndexOf(ResourceTypeOrder, group.Key) is var index && index >= 0
                ? index
                : ResourceTypeOrder.Length)
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new ApuResourceGroupDto
            {
                ResourceType = group.Key,
                Items = group
                    .Select(line => new ApuResourceItemDto
                    {
                        LineId = line.LineId,
                        ResourceId = line.ResourceId,
                        Description = line.Description,
                        Unit = line.Unit,
                        UnitPrice = line.UnitPrice,
                        Consumption = line.Consumption,
                        WastePercentage = line.WastePercentage,
                        MatrixResourceId = line.IsMatrix ? line.ResourceId : null
                    })
                    .ToList()
            })
            .ToList();
    }

    private static IReadOnlyList<MatrixResourceGroupDto> ToMatrixResourceGroups(
        IReadOnlyList<CostGroupResult> groups,
        IReadOnlyList<MatrixComponentRow> components)
    {
        var componentIdsByResource = components
            .GroupBy(component => component.ComponentResourceId)
            .ToDictionary(group => group.Key, group => group.First().ComponentId);

        return groups
            .Select(group => new MatrixResourceGroupDto
            {
                ResourceType = group.ResourceType,
                Items = group.Items
                    .Select(item => new MatrixComponentItemDto
                    {
                        ComponentId = componentIdsByResource.TryGetValue(item.ResourceId, out var componentId)
                            ? componentId
                            : Guid.Empty,
                        ResourceId = item.ResourceId,
                        Description = item.Description,
                        Unit = item.Unit,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        WastePercentage = item.WastePercentage,
                        Subtotal = item.Subtotal
                    })
                    .ToList()
            })
            .ToList();
    }

}
