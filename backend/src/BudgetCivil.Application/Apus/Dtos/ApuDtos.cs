using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Apus.Dtos;

/// <summary>Contrato v1 de APUs, Matrices, costos y conteo (`D5`, `D-101`, `T-C-05`).</summary>
public sealed class WbsItemReferenceDto
{
    public string Code { get; init; } = string.Empty;

    /// <summary>Solo se informa para APUs del proyecto de la ruta (`SC-APUS-24`, `SC-APUS-89`).</summary>
    public decimal? QuantityWork { get; init; }

    public string? Unit { get; init; }
}

public sealed class ApuResourceItemDto
{
    /// <summary>Identificador de la línea del APU; se usa en `resources` de la edición unificada.</summary>
    public Guid LineId { get; init; }

    public Guid ResourceId { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? Unit { get; init; }

    public decimal? UnitPrice { get; init; }

    /// <summary>Cantidad base de la línea, sin desperdicio ni herramienta menor.</summary>
    public decimal Consumption { get; init; }

    public decimal WastePercentage { get; init; }

    /// <summary>Identificador de la Matriz cuando la línea la referencia (`SC-APUS-88`).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? MatrixResourceId { get; init; }
}

public sealed class ApuResourceGroupDto
{
    public string ResourceType { get; init; } = string.Empty;

    public IReadOnlyList<ApuResourceItemDto> Items { get; init; } = [];
}

public sealed class ApuDetailResponse
{
    public Guid ApuId { get; init; }

    public string Description { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public WbsItemReferenceDto? WbsItem { get; init; }

    public IReadOnlyList<ApuResourceGroupDto> Resources { get; init; } = [];

    public string RowVersion { get; init; } = string.Empty;

    public Guid LastModifiedBy { get; init; }

    public DateTime LastModifiedAt { get; init; }
}

public sealed class ApuListItemDto
{
    public Guid ApuId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    /// <summary>Costo directo unitario del APU (`REQ-APUS-05`, `D5`).</summary>
    public decimal? UnitPrice { get; init; }

    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = string.Empty;

    public string? MunicipalityName { get; init; }

    public string? DepartmentName { get; init; }
}

public sealed class ApuListResponse
{
    public IReadOnlyList<ApuListItemDto> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}

public sealed class MatrixComponentItemDto
{
    public Guid ComponentId { get; init; }

    public Guid ResourceId { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? Unit { get; init; }

    public decimal? UnitPrice { get; init; }

    public decimal Quantity { get; init; }

    public decimal WastePercentage { get; init; }

    public decimal? Subtotal { get; init; }
}

public sealed class MatrixResourceGroupDto
{
    public string ResourceType { get; init; } = string.Empty;

    public IReadOnlyList<MatrixComponentItemDto> Items { get; init; } = [];
}

public sealed class MatrixDetailResponse
{
    public Guid MatrixResourceId { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? Unit { get; init; }

    public string Status { get; init; } = string.Empty;

    public IReadOnlyList<MatrixResourceGroupDto> Resources { get; init; } = [];

    public decimal? DirectCost { get; init; }

    public string RowVersion { get; init; } = string.Empty;
}

public sealed class ResourceUsageItemDto
{
    public Guid ResourceId { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? Unit { get; init; }

    /// <summary>Precio unitario del insumo en el proyecto; `null` si no tiene precio.</summary>
    public decimal? UnitPrice { get; init; }

    /// <summary>Cantidad física calculada con desperdicio y expansión de un nivel, sin `yield`.</summary>
    public decimal Quantity { get; init; }

    /// <summary>`ROUND(quantity × unitPrice, roundingDecimals)`; `null` si falta el precio.</summary>
    public decimal? TotalPrice { get; init; }
}

public sealed class ResourceUsageGroupDto
{
    public string ResourceType { get; init; } = string.Empty;

    public string ResourceTypeName { get; init; } = string.Empty;

    /// <summary>Suma de `totalPrice` del tipo; `null` si algún insumo no tiene precio.</summary>
    public decimal? Subtotal { get; init; }

    public IReadOnlyList<ResourceUsageItemDto> Items { get; init; } = [];
}

public sealed class ResourceUsageResponse
{
    public Guid ProjectId { get; init; }

    /// <summary>Insumos agrupados por tipo, en el orden fijo de `ResourceTypeCatalog`.</summary>
    public IReadOnlyList<ResourceUsageGroupDto> Resources { get; init; } = [];

    /// <summary>Suma de los subtotales; `null` si algún grupo está incompleto por falta de precio.</summary>
    public decimal? TotalCost { get; init; }
}
