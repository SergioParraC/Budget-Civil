using BudgetCivil.Application.Calculation;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-25`: agregación B del capítulo (`REQ-EDT-23`, `D2`).</summary>
public sealed class WbsCostAggregationTests
{
    private static readonly Guid Root = Guid.NewGuid();
    private static readonly Guid Child = Guid.NewGuid();
    private static readonly Guid GrandChild = Guid.NewGuid();

    [Fact]
    public void El_capitulo_acumula_los_items_del_subarbol_una_sola_vez()
    {
        // SC-EDT-93.
        var chapters = new[]
        {
            new ChapterCostNode(Root, null),
            new ChapterCostNode(Child, Root),
            new ChapterCostNode(GrandChild, Child)
        };

        var items = new[]
        {
            new ChapterCostItem(Root, 1000m),
            new ChapterCostItem(Child, 100m),
            new ChapterCostItem(GrandChild, 25m)
        };

        var costs = WbsCostAggregation.AggregateChapterCosts(chapters, items, 2);

        costs[GrandChild].Should().Be(25m);
        costs[Child].Should().Be(125m);
        costs[Root].Should().Be(1125m);
    }

    [Fact]
    public void Un_capitulo_sin_items_devuelve_cero()
    {
        // SC-EDT-94 y SC-EDT-99.
        var chapters = new[] { new ChapterCostNode(Root, null) };

        var costs = WbsCostAggregation.AggregateChapterCosts(chapters, [], 2);

        costs[Root].Should().Be(0m);
    }

    [Fact]
    public void Un_item_incompleto_anula_el_capitulo_y_sus_ancestros()
    {
        // SC-EDT-95.
        var chapters = new[]
        {
            new ChapterCostNode(Root, null),
            new ChapterCostNode(Child, Root)
        };

        var items = new[]
        {
            new ChapterCostItem(Root, 1000m),
            new ChapterCostItem(Child, null)
        };

        var costs = WbsCostAggregation.AggregateChapterCosts(chapters, items, 2);

        costs[Child].Should().BeNull();
        costs[Root].Should().BeNull();
    }

    [Fact]
    public void El_item_raiz_no_entra_en_el_subtotal_de_ningun_capitulo()
    {
        // SC-EDT-97.
        var chapters = new[] { new ChapterCostNode(Root, null) };
        var items = new[] { new ChapterCostItem(Root, 50m) };

        var costs = WbsCostAggregation.AggregateChapterCosts(chapters, items, 2);

        costs[Root].Should().Be(50m);
    }

    [Fact]
    public void El_subtotal_del_capitulo_se_redondea_con_los_decimales_de_la_configuracion()
    {
        // SC-EDT-98.
        var chapters = new[] { new ChapterCostNode(Root, null) };
        var items = new[] { new ChapterCostItem(Root, 1.004m) };

        var costs = WbsCostAggregation.AggregateChapterCosts(chapters, items, 2);

        costs[Root].Should().Be(1.00m);
    }
}
