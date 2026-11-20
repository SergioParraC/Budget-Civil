using BudgetCivil.Domain.Catalogs;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Domain;

/// <summary>`T-U-28`: entidad y reglas del catálogo global de unidades (`REQ-UNIDADES-01`).</summary>
public sealed class MeasurementUnitTests
{
    [Fact]
    public void Create_recorta_la_sigla_y_el_nombre()
    {
        var unit = MeasurementUnit.Create(" M2 ", " Metro cuadrado ");

        unit.ShortUnit.Should().Be("M2");
        unit.Unit.Should().Be("Metro cuadrado");
    }

    [Fact]
    public void La_sigla_exige_entre_uno_y_diez_caracteres()
    {
        var empty = () => MeasurementUnit.NormalizeShortUnit("   ");
        var tooLong = () => MeasurementUnit.NormalizeShortUnit(new string('x', MeasurementUnit.ShortUnitMaxLength + 1));

        empty.Should().Throw<InvalidOperationException>();
        tooLong.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void El_nombre_exige_entre_uno_y_cien_caracteres()
    {
        var empty = () => MeasurementUnit.NormalizeUnit("   ");
        var tooLong = () => MeasurementUnit.NormalizeUnit(new string('x', MeasurementUnit.UnitMaxLength + 1));

        empty.Should().Throw<InvalidOperationException>();
        tooLong.Should().Throw<InvalidOperationException>();
    }
}