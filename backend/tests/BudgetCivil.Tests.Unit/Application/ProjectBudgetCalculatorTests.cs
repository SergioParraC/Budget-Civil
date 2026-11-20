using BudgetCivil.Application.Calculation;
using BudgetCivil.Application.Calculation.Services;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-05` y `T-U-09`: costo directo, AIU una sola vez, redondeo, nulos y tope (`REQ-CALCULO-01`…`03`).</summary>
public sealed class ProjectBudgetCalculatorTests
{
    private static readonly CostSettings Settings = new(
        RoundingDecimals: 2,
        QuantityDecimals: 2,
        AiuAdminPercent: 10m,
        AiuUnforeseenPercent: 5m,
        AiuProfitPercent: 8m);

    [Fact]
    public void El_presupuesto_suma_el_costo_directo_y_aplica_el_aiu_una_sola_vez()
    {
        // SC-CALCULO-01 y SC-CALCULO-05.
        var snapshot = Snapshot(
            Item(unitCost: 100m, quantity: 10m),
            Item(unitCost: 50m, quantity: 2m));

        var result = ProjectBudgetCalculator.Calculate(snapshot);

        result.DirectCost.Should().Be(1100m);
        result.IndirectCosts.Administration.Amount.Should().Be(110m);
        result.IndirectCosts.Unforeseen.Amount.Should().Be(55m);
        result.IndirectCosts.Profit.Amount.Should().Be(88m);
        result.IndirectCosts.Total.Should().Be(253m);
        result.Total.Should().Be(1353m);
        result.IsComplete.Should().BeTrue();
        result.ItemCount.Should().Be(2);
    }

    [Fact]
    public void Un_proyecto_sin_items_devuelve_cero_y_esta_completo()
    {
        // SC-CALCULO-02.
        var result = ProjectBudgetCalculator.Calculate(Snapshot());

        result.DirectCost.Should().Be(0m);
        result.IndirectCosts.Administration.Amount.Should().Be(0m);
        result.IndirectCosts.Total.Should().Be(0m);
        result.Total.Should().Be(0m);
        result.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void Un_item_sin_apu_anula_los_bloques_monetarios()
    {
        // SC-CALCULO-03.
        var sinApu = new ProjectItemCost(
            Guid.NewGuid(), null, 10m, null, 0, null, null, HasApu: false, IsApuComplete: false);

        var result = ProjectBudgetCalculator.Calculate(Snapshot(sinApu));

        result.DirectCost.Should().BeNull();
        result.IndirectCosts.Administration.Amount.Should().BeNull();
        result.IndirectCosts.Total.Should().BeNull();
        result.Total.Should().BeNull();
        result.IsComplete.Should().BeFalse();
        result.ItemsWithoutApu.Should().Be(1);
        result.ItemsWithoutPrice.Should().Be(0);
    }

    [Fact]
    public void Un_apu_incompleto_no_devuelve_total_parcial()
    {
        // SC-CALCULO-04 y SC-CALCULO-06.
        var incompleto = new ProjectItemCost(
            Guid.NewGuid(), Guid.NewGuid(), 2m, null, 0, null, null, HasApu: true, IsApuComplete: false);

        var result = ProjectBudgetCalculator.Calculate(
            Snapshot(Item(unitCost: 100m, quantity: 10m), incompleto));

        result.DirectCost.Should().BeNull();
        result.Total.Should().BeNull();
        result.IsComplete.Should().BeFalse();
        result.ItemsWithoutApu.Should().Be(0);
        result.ItemsWithoutPrice.Should().Be(1);
    }

    [Fact]
    public void El_costo_directo_se_redondea_alejado_de_cero()
    {
        // SC-CALCULO-06.
        var snapshot = Snapshot(Item(unitCost: 0.335m, quantity: 3m));

        var result = ProjectBudgetCalculator.Calculate(snapshot);

        result.DirectCost.Should().Be(1.01m);
    }

    [Theory]
    [InlineData(1500, 1353, false)]
    [InlineData(1353, 1353, false)]
    [InlineData(1000, 1353, true)]
    [InlineData(null, 1353, false)]
    public void La_alerta_de_tope_solo_se_activa_con_total_y_tope(
        int? budgetLimit,
        int expectedTotal,
        bool expectedIsOverBudget)
    {
        // SC-CALCULO-09…12 (T-U-09).
        var snapshot = Snapshot(
            budgetLimit.HasValue ? (decimal?)budgetLimit.Value : null,
            Item(unitCost: 100m, quantity: 10m),
            Item(unitCost: 50m, quantity: 2m));

        var result = ProjectBudgetCalculator.Calculate(snapshot);

        result.Total.Should().Be((decimal)expectedTotal);
        result.IsOverBudget.Should().Be(expectedIsOverBudget);
    }

    [Fact]
    public void Sin_total_calculable_la_alerta_de_tope_es_falsa()
    {
        // SC-CALCULO-13.
        var sinApu = new ProjectItemCost(
            Guid.NewGuid(), null, 1m, null, 0, null, null, HasApu: false, IsApuComplete: false);

        var result = ProjectBudgetCalculator.Calculate(Snapshot(500m, sinApu));

        result.Total.Should().BeNull();
        result.IsOverBudget.Should().BeFalse();
    }

    private static ProjectCostSnapshot Snapshot(
        params ProjectItemCost[] items) => Snapshot(null, items);

    private static ProjectCostSnapshot Snapshot(
        decimal? budgetLimit,
        params ProjectItemCost[] items)
    {
        return new ProjectCostSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Settings,
            budgetLimit,
            BaseCurrency: null,
            items,
            new Dictionary<Guid, ApuCostResult>());
    }

    private static ProjectItemCost Item(decimal unitCost, decimal quantity)
    {
        return new ProjectItemCost(
            Guid.NewGuid(),
            Guid.NewGuid(),
            quantity,
            null,
            0,
            unitCost,
            ApuCostCalculator.Round(quantity * unitCost, Settings.RoundingDecimals),
            HasApu: true,
            IsApuComplete: true);
    }
}
