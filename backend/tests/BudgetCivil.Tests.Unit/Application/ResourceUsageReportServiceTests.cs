using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-U-11`: reporte de uso agrupado por tipo, con precio unitario, total por insumo, subtotal por tipo y
/// total general (`REQ-APUS-12`, `SC-APUS-102`, `D-33`, `D-121`).
/// </summary>
public sealed class ResourceUsageReportServiceTests
{
    private static readonly Guid CementId = Guid.NewGuid();
    private static readonly Guid SandId = Guid.NewGuid();
    private static readonly Guid LaborId = Guid.NewGuid();

    [Fact]
    public void El_reporte_agrupa_por_tipo_y_calcula_precio_unitario_y_totales()
    {
        var resources = new[]
        {
            Entry(CementId, "Cemento", "kg", "MATERIAL", "Materiales", 100m),
            Entry(SandId, "Arena", "m3", "MATERIAL", "Materiales", 50m),
            Entry(LaborId, "Oficial", "jornal", "LABOR", "Mano de obra", 200m)
        };

        var report = ResourceUsageReportService.Build(
            resources,
            new Dictionary<Guid, decimal>
            {
                [CementId] = 2m,
                [SandId] = 3m,
                [LaborId] = 1m
            },
            roundingDecimals: 2);

        report.Resources.Select(group => group.ResourceType)
            .Should().ContainInOrder("MATERIAL", "LABOR");

        var material = report.Resources[0];
        material.ResourceTypeName.Should().Be("Materiales");
        material.Items.Select(item => item.Description).Should().ContainInOrder("Arena", "Cemento");
        material.Items.Single(item => item.ResourceId == CementId).TotalPrice.Should().Be(200m);
        material.Items.Single(item => item.ResourceId == SandId).TotalPrice.Should().Be(150m);
        material.Subtotal.Should().Be(350m);

        report.Resources[1].Subtotal.Should().Be(200m);
        report.TotalCost.Should().Be(550m);
    }

    [Fact]
    public void Un_insumo_sin_precio_deja_su_total_y_los_subtotales_en_nulo()
    {
        var resources = new[]
        {
            Entry(CementId, "Cemento", "kg", "MATERIAL", "Materiales", 100m),
            Entry(SandId, "Arena", "m3", "MATERIAL", "Materiales", null)
        };

        var report = ResourceUsageReportService.Build(
            resources,
            new Dictionary<Guid, decimal> { [CementId] = 2m, [SandId] = 3m },
            roundingDecimals: 2);

        var material = report.Resources.Single();
        material.Items.Single(item => item.ResourceId == SandId).TotalPrice.Should().BeNull();
        material.Subtotal.Should().BeNull();
        report.TotalCost.Should().BeNull();
    }

    [Fact]
    public void Un_insumo_sin_uso_aparece_con_cantidad_cero_y_total_cero()
    {
        var resources = new[]
        {
            Entry(CementId, "Cemento", "kg", "MATERIAL", "Materiales", 100m)
        };

        var report = ResourceUsageReportService.Build(
            resources,
            new Dictionary<Guid, decimal>(),
            roundingDecimals: 2);

        var item = report.Resources.Single().Items.Single();
        item.Quantity.Should().Be(0m);
        item.UnitPrice.Should().Be(100m);
        item.TotalPrice.Should().Be(0m);
        report.TotalCost.Should().Be(0m);
    }

    [Fact]
    public void El_total_por_insumo_usa_los_decimales_de_redondeo_del_proyecto()
    {
        var resources = new[]
        {
            Entry(CementId, "Cemento", "kg", "MATERIAL", "Materiales", 2.5m)
        };

        var report = ResourceUsageReportService.Build(
            resources,
            new Dictionary<Guid, decimal> { [CementId] = 1.005m },
            roundingDecimals: 2);

        report.Resources.Single().Items.Single().TotalPrice.Should().Be(2.51m);
    }

    private static ResourceUsageEntry Entry(
        Guid id,
        string description,
        string? unit,
        string typeCode,
        string typeName,
        decimal? unitPrice)
    {
        return new ResourceUsageEntry(id, description, unit, typeCode, typeName, unitPrice);
    }
}
