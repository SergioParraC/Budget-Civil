using BudgetCivil.Domain.Projects;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Domain;

public sealed class ProjectConfigurationTests
{
    [Fact]
    public void NormalizeBudgetLimit_admite_ausencia_y_redondea_a_dos_decimales()
    {
        ProjectConfiguration.NormalizeBudgetLimit(null).Should().BeNull();
        ProjectConfiguration.NormalizeBudgetLimit(1234.567m).Should().Be(1234.57m);
        ProjectConfiguration.NormalizeBudgetLimit(0.001m).Should().Be(0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NormalizeBudgetLimit_rechaza_valores_no_positivos(decimal budgetLimit)
    {
        var action = () => ProjectConfiguration.NormalizeBudgetLimit(budgetLimit);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NormalizeAiuPercent_admite_ausencia_y_redondea()
    {
        ProjectConfiguration.NormalizeAiuPercent(null, "aiu_admin_percent").Should().BeNull();
        ProjectConfiguration.NormalizeAiuPercent(0m, "aiu_admin_percent").Should().Be(0m);
        ProjectConfiguration.NormalizeAiuPercent(100m, "aiu_admin_percent").Should().Be(100m);
        ProjectConfiguration.NormalizeAiuPercent(12.345m, "aiu_admin_percent").Should().Be(12.35m);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void NormalizeAiuPercent_rechaza_valores_fuera_de_rango(decimal aiuPercent)
    {
        var action = () => ProjectConfiguration.NormalizeAiuPercent(aiuPercent, "aiu_admin_percent");

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void La_configuracion_nueva_no_exige_tope_ni_auditor()
    {
        var configuration = new ProjectConfiguration();

        configuration.BudgetLimit.Should().BeNull();
        configuration.RequiresAuditor.Should().BeFalse();
        configuration.SignatureMode.Should().Be(Project.DefaultSignatureMode);
        configuration.SignerUserId.Should().BeNull();
    }
}
