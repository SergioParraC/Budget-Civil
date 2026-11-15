using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-U-03`: reconciliación `EXACT`/`SIMILAR`/`NONE` de insumos simples (`REQ-APUS-10`, `SC-APUS-48`).
/// </summary>
public sealed class ApuCopyReconciliationTests
{
    [Fact]
    public void Un_insumo_con_la_misma_descripcion_y_unidad_es_exacto()
    {
        var source = Resource(Guid.NewGuid(), "Cemento gris", "kg", 1200m);
        var candidate = Resource(Guid.NewGuid(), "  Cemento gris ", "KG", 1200m);

        var result = ApuCopyReconciliation.Classify(source, [candidate]);

        result.Match.Should().Be(ApuCopyReconciliation.Exact);
        result.Candidates.Should().ContainSingle()
            .Which.ResourceId.Should().Be(candidate.ResourceId);
    }

    [Fact]
    public void El_match_exacto_compara_unidad_y_precio_aunque_difieran()
    {
        // La descripción coincide, pero la unidad y el precio del destino no: el cliente debe verlo.
        var source = Resource(Guid.NewGuid(), "Cemento gris", "kg", 1200m);
        var candidate = Resource(Guid.NewGuid(), "Cemento gris", "bulto", 50000m);

        var match = ApuCopyReconciliation.Classify(source, [candidate]);
        var comparison = ApuCopyReconciliation.Compare(
            source.Unit,
            source.UnitPrice,
            candidate.Unit,
            candidate.UnitPrice);

        match.Match.Should().Be(ApuCopyReconciliation.Exact);
        comparison.UnitMatches.Should().BeFalse();
        comparison.UnitPriceMatches.Should().BeFalse();
        comparison.UnitPriceDifference.Should().Be(48800m);
    }

    [Fact]
    public void El_comparativo_marca_coincidencias_y_nulos()
    {
        var same = ApuCopyReconciliation.Compare("kg", 1200m, "KG", 1200m);
        same.UnitMatches.Should().BeTrue();
        same.UnitPriceMatches.Should().BeTrue();
        same.UnitPriceDifference.Should().Be(0m);

        var bothNull = ApuCopyReconciliation.Compare("kg", null, "kg", null);
        bothNull.UnitMatches.Should().BeTrue();
        bothNull.UnitPriceMatches.Should().BeTrue();
        bothNull.UnitPriceDifference.Should().BeNull();

        var missingPrice = ApuCopyReconciliation.Compare("kg", 1200m, "kg", null);
        missingPrice.UnitPriceMatches.Should().BeFalse();
        missingPrice.UnitPriceDifference.Should().BeNull();
    }

    [Fact]
    public void Una_descripcion_parcial_es_similar_y_devuelve_candidatos()
    {
        var source = Resource(Guid.NewGuid(), "Cemento gris", "kg", 1200m);
        var candidate = Resource(Guid.NewGuid(), "Cemento gris de alta resistencia", "kg", 1300m);

        var result = ApuCopyReconciliation.Classify(source, [candidate]);

        result.Match.Should().Be(ApuCopyReconciliation.Similar);
        result.Candidates.Should().ContainSingle()
            .Which.ResourceId.Should().Be(candidate.ResourceId);
    }

    [Fact]
    public void Sin_coincidencias_el_insumo_es_nuevo()
    {
        var source = Resource(Guid.NewGuid(), "Cemento gris", "kg", 1200m);
        var candidate = Resource(Guid.NewGuid(), "Arena fina", "m3", 5000m);

        var result = ApuCopyReconciliation.Classify(source, [candidate]);

        result.Match.Should().Be(ApuCopyReconciliation.None);
        result.Candidates.Should().BeEmpty();
    }

    [Fact]
    public void Los_candidatos_se_limitan_a_diez()
    {
        var source = Resource(Guid.NewGuid(), "Cemento", "kg", 1200m);
        var candidates = Enumerable.Range(0, 15)
            .Select(index => Resource(Guid.NewGuid(), $"Cemento {index}", "kg", 1200m))
            .ToList();

        var result = ApuCopyReconciliation.Classify(source, candidates);

        result.Candidates.Should().HaveCount(ApuCopyReconciliation.MaxCandidates);
    }

    private static SimpleResourceSnapshot Resource(Guid id, string description, string? unit, decimal? unitPrice)
    {
        return new SimpleResourceSnapshot(
            id,
            "MATERIAL",
            description,
            unit,
            unitPrice,
            "POR_COTIZAR");
    }
}
