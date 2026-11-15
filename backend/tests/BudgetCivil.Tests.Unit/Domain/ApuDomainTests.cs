using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Wbs;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Domain;

/// <summary>
/// `T-U-21`, `T-U-04` y `T-U-22`: completitud, composición, Matrices y vínculo del ítem (`REQ-APUS-01`
/// …`04`, `REQ-APUS-06`, `07`, `14`, `REQ-EDT-20`).
/// </summary>
public sealed class ApuDomainTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    [Fact]
    public void Un_apu_sin_lineas_queda_falta_informacion()
    {
        // SC-APUS-11 y SC-APUS-96.
        var apu = CreateApu();

        apu.CalculateStatus(referencedMatricesComplete: true).Should().Be(ApuStatus.FaltaInformacion);
    }

    [Fact]
    public void Un_apu_con_descripcion_unidad_rendimiento_y_linea_queda_completo()
    {
        // SC-APUS-12.
        var apu = CreateApu();
        apu.AddLine(Guid.NewGuid(), 2m, 0m, ActorId, DateTime.UtcNow);

        apu.CalculateStatus(referencedMatricesComplete: true).Should().Be(ApuStatus.Completo);
    }

    [Fact]
    public void Una_matriz_incompleta_hace_que_el_apu_herede_falta_informacion()
    {
        // SC-APUS-13, SC-APUS-97 e INC-06.
        var apu = CreateApu();
        apu.AddLine(Guid.NewGuid(), 1m, 0m, ActorId, DateTime.UtcNow);

        apu.CalculateStatus(referencedMatricesComplete: false).Should().Be(ApuStatus.FaltaInformacion);
    }

    [Fact]
    public void El_alta_valida_descripcion_y_unidad()
    {
        // SC-APUS-08.
        var invalidUnit = () => Apu.Create(ProjectId, "Excavación", "metros", ActorId, DateTime.UtcNow);
        var invalidDescription = () => Apu.Create(ProjectId, "  ", "m3", ActorId, DateTime.UtcNow);

        invalidUnit.Should().Throw<InvalidOperationException>();
        invalidDescription.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void La_linea_exige_consumo_positivo_y_desperdicio_entre_cero_y_cien()
    {
        // SC-APUS-14 y SC-APUS-29.
        var apu = CreateApu();

        var zeroConsumption = () => apu.AddLine(Guid.NewGuid(), 0m, 0m, ActorId, DateTime.UtcNow);
        var invalidWaste = () => apu.AddLine(Guid.NewGuid(), 1m, 101m, ActorId, DateTime.UtcNow);

        zeroConsumption.Should().Throw<InvalidOperationException>();
        invalidWaste.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void La_composicion_actualiza_la_autoria_del_apu()
    {
        // SC-APUS-64.
        var apu = CreateApu();
        var secondActor = Guid.NewGuid();
        var later = DateTime.UtcNow.AddMinutes(5);

        apu.AddLine(Guid.NewGuid(), 1m, 0m, secondActor, later);

        apu.LastModifiedByUserId.Should().Be(secondActor);
        apu.LastModifiedAtUtc.Should().Be(later);
    }

    [Fact]
    public void Un_componente_de_matriz_no_puede_ser_ella_misma()
    {
        // SC-APUS-33.
        var matrixId = Guid.NewGuid();

        var action = () => MatrixComponent.Create(matrixId, matrixId, 1m, 0m);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Un_componente_valido_conserva_consumo_y_desperdicio()
    {
        // SC-APUS-26 y SC-APUS-30.
        var component = MatrixComponent.Create(Guid.NewGuid(), Guid.NewGuid(), 2m, 5m);

        component.Consumption.Should().Be(2m);
        component.WastePercentage.Should().Be(5m);
    }

    [Fact]
    public void El_item_solo_se_vincula_una_vez_y_exige_un_apu()
    {
        // SC-APUS-01, SC-APUS-02 y T-U-22.
        var item = CreateItem();
        var apuId = Guid.NewGuid();

        item.LinkApu(apuId);
        item.ApuId.Should().Be(apuId);

        var secondLink = () => item.LinkApu(Guid.NewGuid());
        var emptyLink = () => CreateItem().LinkApu(Guid.Empty);

        secondLink.Should().Throw<InvalidOperationException>();
        emptyLink.Should().Throw<InvalidOperationException>();
    }

    private static Apu CreateApu()
    {
        return Apu.Create(ProjectId, "Excavación", "m3", ActorId, DateTime.UtcNow);
    }

    private static WbsItem CreateItem()
    {
        return WbsItem.Create(
            ProjectId,
            null,
            WbsCode.Create(ProjectId, "1.1", WbsNodeType.Item),
            10m,
            0,
            ActorId,
            DateTime.UtcNow);
    }
}
