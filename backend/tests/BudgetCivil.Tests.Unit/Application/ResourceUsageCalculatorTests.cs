using BudgetCivil.Application.Calculation;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-U-11`: conteo de uso con desperdicio y expansión de un nivel (`REQ-APUS-12`, `REQ-APUS-13`,
/// `R-21`, `D-33`).
/// </summary>
public sealed class ResourceUsageCalculatorTests
{
    private static readonly Guid MaterialId = Guid.NewGuid();
    private static readonly Guid MatrixId = Guid.NewGuid();

    [Fact]
    public void El_consumo_directo_se_multiplica_por_la_cantidad_de_obra()
    {
        // SC-APUS-56.
        var usage = Calculate([Direct(MaterialId, 2m, 0m, 10m)], [], []);

        usage[MaterialId].Should().Be(20m);
    }

    [Fact]
    public void El_mismo_insumo_en_dos_apus_se_suma()
    {
        // SC-APUS-57.
        var usage = Calculate(
            [Direct(MaterialId, 1m, 0m, 5m), Direct(MaterialId, 3m, 0m, 2m)],
            [],
            []);

        usage[MaterialId].Should().Be(11m);
    }

    [Fact]
    public void El_desperdicio_de_la_linea_interviene()
    {
        // SC-APUS-58.
        var usage = Calculate([Direct(MaterialId, 2m, 10m, 10m)], [], []);

        usage[MaterialId].Should().Be(22m);
    }

    [Fact]
    public void Un_insumo_sin_uso_no_aparece_en_el_calculo()
    {
        // SC-APUS-59: la consulta lo devuelve con cero.
        var usage = Calculate([], [], []);

        usage.Should().NotContainKey(MaterialId);
    }

    [Fact]
    public void La_matriz_expande_sus_componentes_un_solo_nivel()
    {
        // SC-APUS-60 y SC-APUS-62.
        var componentA = Guid.NewGuid();
        var componentB = Guid.NewGuid();
        var usage = Calculate(
            [],
            [MatrixLine(MatrixId, 4m, 0m, 10m)],
            [
                new UsageMatrixComponent(MatrixId, componentA, 2m, 0m),
                new UsageMatrixComponent(MatrixId, componentB, 3m, 0m)
            ]);

        usage[componentA].Should().Be(80m);
        usage[componentB].Should().Be(120m);
        usage[MatrixId].Should().Be(40m);
    }

    [Fact]
    public void La_matriz_usada_en_dos_apus_suma_sus_cantidades()
    {
        // SC-APUS-61.
        var component = Guid.NewGuid();
        var usage = Calculate(
            [],
            [MatrixLine(MatrixId, 1m, 0m, 5m), MatrixLine(MatrixId, 1m, 0m, 2m)],
            [new UsageMatrixComponent(MatrixId, component, 3m, 0m)]);

        usage[component].Should().Be(21m);
        usage[MatrixId].Should().Be(7m);
    }

    [Fact]
    public void El_desperdicio_se_aplica_en_la_linea_y_en_el_componente()
    {
        // SC-APUS-98.
        var component = Guid.NewGuid();
        var usage = Calculate(
            [],
            [MatrixLine(MatrixId, 4m, 5m, 10m)],
            [new UsageMatrixComponent(MatrixId, component, 2m, 10m)]);

        usage[MatrixId].Should().Be(42m);
        usage[component].Should().Be(92.4m);
    }

    private static IReadOnlyDictionary<Guid, decimal> Calculate(
        IReadOnlyList<UsageDirectLine> direct,
        IReadOnlyList<UsageMatrixLine> matrix,
        IReadOnlyList<UsageMatrixComponent> components)
    {
        return ResourceUsageCalculator.Calculate(direct, matrix, components);
    }

    private static UsageDirectLine Direct(Guid resourceId, decimal consumption, decimal waste, decimal quantity)
        => new(resourceId, consumption, waste, quantity);

    private static UsageMatrixLine MatrixLine(Guid resourceId, decimal consumption, decimal waste, decimal quantity)
        => new(resourceId, consumption, waste, quantity);
}
