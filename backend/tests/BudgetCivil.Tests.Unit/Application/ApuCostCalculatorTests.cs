using BudgetCivil.Application.Calculation;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-U-24`: costo directo, subtotales por tipo, AIU y redondeo (`REQ-APUS-19`, `REQ-APUS-20`, `D8`,
/// `D-121`). El rendimiento se retiró en `REQ-APUS-22`.
/// </summary>
public sealed class ApuCostCalculatorTests
{
    private static readonly CostSettings Settings = new(
        RoundingDecimals: 2,
        QuantityDecimals: 2,
        AiuAdminPercent: 10m,
        AiuUnforeseenPercent: 5m,
        AiuProfitPercent: 8m);

    [Fact]
    public void El_costo_directo_y_el_aiu_se_calculan_por_linea()
    {
        // SC-APUS-86.
        var line = Line("MATERIAL", 2m, 0m, 100m);
        var result = ApuCostCalculator.CalculateApu([line], Settings);

        result.DirectCost.Should().Be(200m);
        result.IndirectCosts.Administration.Amount.Should().Be(20m);
        result.IndirectCosts.Unforeseen.Amount.Should().Be(10m);
        result.IndirectCosts.Profit.Amount.Should().Be(16m);
        result.IndirectCosts.Total.Should().Be(46m);
        result.Total.Should().Be(246m);
        result.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void Los_insumos_se_agrupan_por_tipo_y_su_suma_es_el_costo_directo()
    {
        // SC-APUS-87.
        var result = ApuCostCalculator.CalculateApu(
            [
                Line("MATERIAL", 1m, 0m, 100m),
                Line("LABOR", 2m, 0m, 50m),
                Line("EQUIPMENT", 1m, 0m, 25m)
            ],
            
            Settings);

        result.Resources.Select(group => group.ResourceType)
            .Should().ContainInOrder("MATERIAL", "LABOR", "EQUIPMENT");
        result.Resources.Sum(group => group.Items.Sum(item => item.Subtotal ?? 0m))
            .Should().Be(result.DirectCost);
    }

    [Fact]
    public void Un_insumo_sin_precio_deja_los_costos_en_nulo()
    {
        // SC-APUS-90.
        var result = ApuCostCalculator.CalculateApu(
            [Line("MATERIAL", 1m, 0m, null)],
            
            Settings);

        result.DirectCost.Should().BeNull();
        result.IndirectCosts.Total.Should().BeNull();
        result.Total.Should().BeNull();
        result.IsComplete.Should().BeFalse();
        result.Resources.Single().Items.Single().Subtotal.Should().BeNull();
    }

    [Fact]
    public void La_linea_de_matriz_usa_el_costo_directo_de_la_matriz()
    {
        // SC-APUS-88.
        var matrixLine = new ApuCostLineInput(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "MATRIX",
            "Mortero",
            "m3",
            null,
            1m,
            0m,
            [new MatrixComponentInput(Guid.NewGuid(), "MATERIAL", "Arena", "m3", 500m, 2m, 0m)]);

        var result = ApuCostCalculator.CalculateApu([matrixLine], Settings);

        result.Resources.Single().ResourceType.Should().Be("MATRIX");
        result.Resources.Single().Items.Single().MatrixResourceId.Should().Be(matrixLine.ResourceId);
        result.DirectCost.Should().Be(1000m);
    }

    [Fact]
    public void El_costo_de_la_matriz_aplica_desperdicio_y_no_aiu()
    {
        // SC-APUS-92 y SC-APUS-93.
        var result = ApuCostCalculator.CalculateMatrix(
            [new MatrixComponentInput(Guid.NewGuid(), "MATERIAL", "Arena", "m3", 10m, 2m, 10m)],
            Settings);

        result.Resources.Single().Items.Single().Quantity.Should().Be(2.2m);
        result.Resources.Single().Items.Single().Subtotal.Should().Be(22m);
        result.DirectCost.Should().Be(22m);
    }

    [Fact]
    public void La_matriz_sin_precio_queda_incompleta()
    {
        var result = ApuCostCalculator.CalculateMatrix(
            [new MatrixComponentInput(Guid.NewGuid(), "MATERIAL", "Arena", "m3", null, 1m, 0m)],
            Settings);

        result.DirectCost.Should().BeNull();
        result.IsComplete.Should().BeFalse();
    }

    private static ApuCostLineInput Line(
        string type,
        decimal consumption,
        decimal waste,
        decimal? unitPrice)
    {
        return new ApuCostLineInput(
            Guid.NewGuid(),
            Guid.NewGuid(),
            type,
            "Insumo",
            "un",
            unitPrice,
            consumption,
            waste);
    }
}
