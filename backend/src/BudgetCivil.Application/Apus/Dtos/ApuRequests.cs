namespace BudgetCivil.Application.Apus.Dtos;

public sealed class AddMatrixComponentRequest
{
    public Guid ComponentResourceId { get; init; }

    public decimal Consumption { get; init; }

    public decimal WastePercentage { get; init; }

    public string? RowVersion { get; init; }
}

public sealed class UpdateMatrixComponentRequest
{
    public decimal Consumption { get; init; }

    public decimal WastePercentage { get; init; }

    public string? RowVersion { get; init; }
}

public sealed class ApuCopyResolutionRequest
{
    public Guid SourceResourceId { get; init; }

    /// <summary>`USE_EXISTING` o `KEEP_ORIGIN` (`D9`).</summary>
    public string Decision { get; init; } = string.Empty;

    public Guid? ExistingResourceId { get; init; }
}

public sealed class ApuCopyComparisonDto
{
    /// <summary>La unidad del insumo de origen coincide con la del candidato del destino.</summary>
    public bool UnitMatches { get; init; }

    /// <summary>El precio unitario del origen coincide con el del candidato; ambos nulos también coinciden.</summary>
    public bool UnitPriceMatches { get; init; }

    /// <summary>Diferencia `candidato − origen`; nula si falta alguno de los dos precios.</summary>
    public decimal? UnitPriceDifference { get; init; }
}

public sealed class ApuCopyCandidateDto
{
    public Guid ResourceId { get; init; }

    public string Description { get; init; } = string.Empty;

    public string ResourceTypeCode { get; init; } = string.Empty;

    public string? Unit { get; init; }

    public decimal? UnitPrice { get; init; }

    public string QuotationStatus { get; init; } = string.Empty;

    /// <summary>Comparativo de unidad y precio unitario contra el insumo de origen.</summary>
    public ApuCopyComparisonDto Comparison { get; init; } = new();
}

public sealed class ApuCopyPreviewItemDto
{
    public Guid SourceResourceId { get; init; }

    public string Description { get; init; } = string.Empty;

    public string ResourceTypeCode { get; init; } = string.Empty;

    public string? Unit { get; init; }

    /// <summary>Precio unitario del insumo en el proyecto de origen (`null` si no tiene precio).</summary>
    public decimal? UnitPrice { get; init; }

    /// <summary>`EXACT`, `SIMILAR` o `NONE` (`D9`).</summary>
    public string Match { get; init; } = string.Empty;

    /// <summary>
    /// `true` para `EXACT` y `SIMILAR`: la copia exige una resolución
    /// (`USE_EXISTING` o `KEEP_ORIGIN`). En `NONE` el insumo se copia automáticamente.
    /// </summary>
    public bool RequiresResolution { get; init; }

    public IReadOnlyList<ApuCopyCandidateDto> Candidates { get; init; } = [];
}

public sealed class ApuCopyPreviewResponse
{
    public Guid SourceApuId { get; init; }

    public IReadOnlyList<ApuCopyPreviewItemDto> Items { get; init; } = [];
}

public sealed class CopyMatrixRequest
{
    public Guid SourceMatrixResourceId { get; init; }
}

public sealed class CopyMatrixResponse
{
    public Guid MatrixResourceId { get; init; }
}
