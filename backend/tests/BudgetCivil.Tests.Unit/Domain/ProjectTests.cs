using BudgetCivil.Domain.Projects;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Domain;

public sealed class ProjectTests
{
    [Fact]
    public void NormalizeName_recorta_y_conserva_un_nombre_valido()
    {
        Project.NormalizeName("  Torre Norte  ").Should().Be("Torre Norte");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NormalizeName_rechaza_un_nombre_vacio(string? name)
    {
        var action = () => Project.NormalizeName(name!);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NormalizeName_rechaza_un_nombre_demasiado_largo()
    {
        var action = () => Project.NormalizeName(new string('a', Project.NameMaxLength + 1));

        action.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("  P-001  ", "P-001")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void NormalizeCode_normaliza_el_codigo(string? code, string? expected)
    {
        Project.NormalizeCode(code).Should().Be(expected);
    }

    [Fact]
    public void NormalizeCode_rechaza_un_codigo_demasiado_largo()
    {
        var action = () => Project.NormalizeCode(new string('a', Project.CodeMaxLength + 1));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NormalizeDescription_y_Objective_recortan_y_limitan_longitud()
    {
        Project.NormalizeDescription(" descripcion ").Should().Be("descripcion");
        Project.NormalizeObjective(" objetivo ").Should().Be("objetivo");

        var action = () => Project.NormalizeDescription(new string('a', Project.DescriptionMaxLength + 1));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Hay_exactamente_tres_modos_de_firma()
    {
        // D-03/D-90.
        Project.SignatureModes.Should().HaveCount(3);
        Project.SignatureModes.Should().Contain(Project.NoSignature);
        Project.SignatureModes.Should().Contain(Project.UniqueSignerByAssignedEstimator);
        Project.SignatureModes.Should().Contain(Project.IndependentSignerByResponsibleProfessional);
    }

    [Theory]
    [InlineData("SIN_FIRMA")]
    [InlineData("unica_por_presupuestador_asignado")]
    [InlineData("INDEPENDIENTE_POR_PROFESIONAL_RESPONSABLE")]
    public void IsValidSignatureMode_acepta_los_tres_modos(string signatureMode)
    {
        Project.IsValidSignatureMode(signatureMode).Should().BeTrue();
    }

    [Theory]
    [InlineData("FIRMA_DEL_DIRECTOR")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValidSignatureMode_rechaza_un_modo_desconocido(string? signatureMode)
    {
        Project.IsValidSignatureMode(signatureMode).Should().BeFalse();
    }

    [Fact]
    public void NormalizeSignatureModeOrDefault_usa_el_modo_por_defecto()
    {
        Project.NormalizeSignatureModeOrDefault(null).Should().Be(Project.DefaultSignatureMode);
        Project.NormalizeSignatureModeOrDefault("  ").Should().Be(Project.DefaultSignatureMode);
    }

    [Fact]
    public void NormalizeSignatureModeOrDefault_normaliza_y_rechaza_invalidos()
    {
        Project.NormalizeSignatureModeOrDefault(" sin_firma ").Should().Be(Project.NoSignature);

        var action = () => Project.NormalizeSignatureModeOrDefault("OTRO");

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Un_proyecto_nuevo_queda_en_por_cotizar_y_privado_por_defecto()
    {
        var project = new Project { Name = "Proyecto" };

        project.Status.Should().Be(ProjectStatus.PorCotizar);
        project.IsPublic.Should().BeFalse();
    }
}
