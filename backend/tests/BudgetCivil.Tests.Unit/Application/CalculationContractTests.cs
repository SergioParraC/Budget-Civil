using System.Text.Json;
using BudgetCivil.Application.Calculation.Dtos;
using BudgetCivil.Application.Wbs.Dtos;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-C-06` y `T-C-07`: contrato v1 del presupuesto, del árbol y de la cotización masiva (`D-41`, `D-99`).</summary>
public sealed class CalculationContractTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void El_presupuesto_expone_los_campos_del_contrato_en_camel_case()
    {
        // SC-CALCULO-01 y RNF del contrato v1.
        var response = new ProjectBudgetResponse
        {
            ProjectId = Guid.NewGuid(),
            BaseCurrency = new ProjectBudgetCurrencyDto
            {
                Id = Guid.NewGuid(),
                IsoCode = "COP",
                Name = "Peso colombiano"
            },
            RoundingDecimals = 2,
            QuantityDecimals = 2,
            DirectCost = 1100m,
            IndirectCosts = new ProjectBudgetIndirectCostsDto
            {
                Administration = new ProjectBudgetAiuComponentDto { Percent = 10m, Amount = 110m },
                Unforeseen = new ProjectBudgetAiuComponentDto { Percent = 5m, Amount = 55m },
                Profit = new ProjectBudgetAiuComponentDto { Percent = 8m, Amount = 88m },
                Total = 253m
            },
            Total = 1353m,
            BudgetLimit = 1500m,
            IsOverBudget = false,
            IsComplete = true,
            ItemCount = 2,
            ItemsWithoutApu = 0,
            ItemsWithoutPrice = 0
        };

        var json = JsonSerializer.Serialize(response, WebOptions);

        json.Should().Contain("\"projectId\"");
        json.Should().Contain("\"baseCurrency\"");
        json.Should().Contain("\"isoCode\":\"COP\"");
        json.Should().Contain("\"roundingDecimals\":2");
        json.Should().Contain("\"quantityDecimals\":2");
        json.Should().Contain("\"directCost\":1100");
        json.Should().Contain("\"indirectCosts\"");
        json.Should().Contain("\"administration\"");
        json.Should().Contain("\"unforeseen\"");
        json.Should().Contain("\"profit\"");
        json.Should().Contain("\"total\":1353");
        json.Should().Contain("\"budgetLimit\":1500");
        json.Should().Contain("\"isOverBudget\":false");
        json.Should().Contain("\"isComplete\":true");
        json.Should().Contain("\"itemCount\":2");
        json.Should().Contain("\"itemsWithoutApu\":0");
        json.Should().Contain("\"itemsWithoutPrice\":0");
        json.Should().NotContain("direct_cost");
        json.Should().NotContain("\"DirectCost\"");
    }

    [Fact]
    public void El_presupuesto_incompleto_devuelve_los_bloques_monetarios_nulos()
    {
        // SC-CALCULO-03 y SC-CALCULO-04.
        var json = JsonSerializer.Serialize(
            new ProjectBudgetResponse
            {
                ProjectId = Guid.NewGuid(),
                DirectCost = null,
                IndirectCosts = new ProjectBudgetIndirectCostsDto
                {
                    Administration = new ProjectBudgetAiuComponentDto { Percent = 10m, Amount = null },
                    Unforeseen = new ProjectBudgetAiuComponentDto { Percent = 5m, Amount = null },
                    Profit = new ProjectBudgetAiuComponentDto { Percent = 8m, Amount = null },
                    Total = null
                },
                Total = null,
                IsComplete = false,
                ItemsWithoutPrice = 1
            },
            WebOptions);

        json.Should().Contain("\"directCost\":null");
        json.Should().Contain("\"total\":null");
        json.Should().Contain("\"isComplete\":false");
        json.Should().Contain("\"itemsWithoutPrice\":1");
    }

    [Fact]
    public void El_nodo_del_arbol_expone_unitCost_directCost_y_apu_unitPrice()
    {
        // SC-EDT-89 (T-U-26).
        var json = JsonSerializer.Serialize(
            new WbsNodeDto
            {
                Id = Guid.NewGuid(),
                NodeType = "ITEM",
                Code = "1.1",
                UnitCost = 15000m,
                DirectCost = 150000m,
                Apu = new WbsApuReferenceDto
                {
                    Id = Guid.NewGuid(),
                    Status = "POR_COTIZAR",
                    Description = "Concreto 3000 psi",
                    Unit = "M3",
                    UnitPrice = 15000m,
                    RowVersion = "AQ=="
                }
            },
            WebOptions);

        json.Should().Contain("\"unitCost\":15000");
        json.Should().Contain("\"directCost\":150000");
        json.Should().Contain("\"unitPrice\":15000");
        json.Should().NotContain("unit_cost");
    }
}
