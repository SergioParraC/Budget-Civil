namespace BudgetCivil.Application.Apus.Repositories;

/// <summary>Componente de una Matriz con los datos de costo que necesita `D8`.</summary>
public sealed record MatrixComponentSource(
    Guid ComponentResourceId,
    string ResourceTypeCode,
    string Description,
    string? Unit,
    decimal? UnitPrice,
    decimal Consumption,
    decimal WastePercentage);

/// <summary>Línea de composición del APU con su Matriz expandida cuando corresponde.</summary>
public sealed record ApuCostLineSource(
    Guid LineId,
    Guid ResourceId,
    string ResourceTypeCode,
    string Description,
    string? Unit,
    decimal? UnitPrice,
    decimal Consumption,
    decimal WastePercentage,
    IReadOnlyList<MatrixComponentSource> MatrixComponents,
    bool MatrixComplete = true)
{
    public bool IsMatrix =>
        string.Equals(ResourceTypeCode, "MATRIX", StringComparison.OrdinalIgnoreCase);
}

public sealed record ApuCostPayload(
    Guid ApuId,
    Guid ProjectId,
    IReadOnlyList<ApuCostLineSource> Lines);

public sealed record ApuListRow(
    Guid ApuId,
    string ItemCode,
    string Description,
    string Unit,
    Guid ProjectId,
    string ProjectName,
    string? MunicipalityName,
    string? DepartmentName,
    IReadOnlyList<ApuCostLineSource> Lines);

public sealed record ApuListPage(
    int Page,
    int PageSize,
    int TotalItems,
    IReadOnlyList<ApuListRow> Items);

/// <summary>Estado de un insumo Matriz para validar tipo, unidad, componentes y concurrencia.</summary>
public sealed record MatrixStateSnapshot(
    Guid MatrixResourceId,
    Guid ProjectId,
    string Description,
    string? Unit,
    string ResourceTypeCode,
    int ComponentCount,
    byte[] RowVersion)
{
    public bool IsComplete => !string.IsNullOrWhiteSpace(Unit) && ComponentCount > 0;
}

public sealed record MatrixComponentRow(
    Guid ComponentId,
    Guid MatrixResourceId,
    Guid ComponentResourceId,
    string ComponentResourceTypeCode,
    string Description,
    string? Unit,
    decimal? UnitPrice,
    decimal Consumption,
    decimal WastePercentage);

public sealed record ProjectCostSettings(
    Guid ProjectId,
    Guid CompanyId,
    decimal AiuAdminPercent,
    decimal AiuUnforeseenPercent,
    decimal AiuProfitPercent,
    int RoundingDecimals,
    int QuantityDecimals);

/// <summary>Insumo del proyecto destino usado por la reconciliación de la copia (`D9`).</summary>
public sealed record SimpleResourceSnapshot(
    Guid ResourceId,
    string ResourceTypeCode,
    string Description,
    string? Unit,
    decimal? UnitPrice,
    string QuotationStatus);

public sealed record UsageDirectLineRow(
    Guid ResourceId,
    decimal Consumption,
    decimal WastePercentage,
    decimal QuantityWork);

public sealed record UsageMatrixLineRow(
    Guid MatrixResourceId,
    decimal Consumption,
    decimal WastePercentage,
    decimal QuantityWork);

public sealed record UsageMatrixComponentRow(
    Guid MatrixResourceId,
    Guid ComponentResourceId,
    decimal Consumption,
    decimal WastePercentage);

public sealed record ResourceUsageEntry(
    Guid ResourceId,
    string Description,
    string? Unit,
    string ResourceTypeCode,
    string ResourceTypeName,
    decimal? UnitPrice);

public sealed record ResourceUsageData(
    IReadOnlyList<ResourceUsageEntry> Resources,
    IReadOnlyList<UsageDirectLineRow> DirectLines,
    IReadOnlyList<UsageMatrixLineRow> MatrixLines,
    IReadOnlyList<UsageMatrixComponentRow> MatrixComponents);
