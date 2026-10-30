using BudgetCivil.Domain.Projects;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Domain;

public sealed class ProjectStatusTransitionPolicyTests
{
    [Theory]
    [InlineData(ProjectStatus.PorCotizar, ProjectStatus.FaltaInformacion, ProjectTransitionKind.Ordinary)]
    [InlineData(ProjectStatus.FaltaInformacion, ProjectStatus.Cotizado, ProjectTransitionKind.Ordinary)]
    [InlineData(ProjectStatus.Cotizado, ProjectStatus.FaltaInformacion, ProjectTransitionKind.Ordinary)]
    [InlineData(ProjectStatus.Cotizado, ProjectStatus.Finalizado, ProjectTransitionKind.Ordinary)]
    [InlineData(ProjectStatus.Finalizado, ProjectStatus.FaltaInformacion, ProjectTransitionKind.Reopen)]
    [InlineData(ProjectStatus.PorCotizar, ProjectStatus.Inhabilitado, ProjectTransitionKind.Disable)]
    [InlineData(ProjectStatus.Finalizado, ProjectStatus.Inhabilitado, ProjectTransitionKind.Disable)]
    public void EnsureTransitionAllowed_devuelve_el_tipo_esperado(
        ProjectStatus current,
        ProjectStatus target,
        ProjectTransitionKind expected)
    {
        var kind = ProjectStatusTransitionPolicy.EnsureTransitionAllowed(current, target, isReactivationEndpoint: false);

        kind.Should().Be(expected);
    }

    [Theory]
    [InlineData(ProjectStatus.PorCotizar, ProjectStatus.Finalizado)]
    [InlineData(ProjectStatus.FaltaInformacion, ProjectStatus.Finalizado)]
    [InlineData(ProjectStatus.PorCotizar, ProjectStatus.Cotizado)]
    [InlineData(ProjectStatus.Finalizado, ProjectStatus.Cotizado)]
    [InlineData(ProjectStatus.Inhabilitado, ProjectStatus.Inhabilitado)]
    [InlineData(ProjectStatus.Inhabilitado, ProjectStatus.Cotizado)]
    [InlineData(ProjectStatus.Cotizado, ProjectStatus.Cotizado)]
    public void EnsureTransitionAllowed_rechaza_parejas_invalidas(ProjectStatus current, ProjectStatus target)
    {
        var action = () => ProjectStatusTransitionPolicy.EnsureTransitionAllowed(current, target, isReactivationEndpoint: false);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reactivacion_solo_desde_inhabilitado_hacia_falta_informacion()
    {
        var kind = ProjectStatusTransitionPolicy.EnsureTransitionAllowed(
            ProjectStatus.Inhabilitado,
            ProjectStatus.FaltaInformacion,
            isReactivationEndpoint: true);

        kind.Should().Be(ProjectTransitionKind.Reactivate);
    }

    [Theory]
    [InlineData(ProjectStatus.Cotizado, ProjectStatus.FaltaInformacion)]
    [InlineData(ProjectStatus.Inhabilitado, ProjectStatus.Cotizado)]
    public void Reactivacion_rechaza_cualquier_otra_pareja(ProjectStatus current, ProjectStatus target)
    {
        var action = () => ProjectStatusTransitionPolicy.EnsureTransitionAllowed(current, target, isReactivationEndpoint: true);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void El_director_cierra_desde_cotizado_sin_otra_precondicion()
    {
        // SC-PROYECTOS-12: el cierre no exige que el Director haya firmado APUs.
        ProjectStatusTransitionPolicy
            .CanRolePerformTransition(
                ProjectStatusTransitionPolicy.DirectorRoleCode,
                ProjectStatus.Cotizado,
                ProjectStatus.Finalizado,
                isReactivationEndpoint: false)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData("PRESUPUESTADOR")]
    [InlineData("AUDITOR")]
    [InlineData("ADMINISTRADOR")]
    public void Otro_rol_no_puede_cerrar(string roleCode)
    {
        // SC-PROYECTOS-14.
        ProjectStatusTransitionPolicy
            .CanRolePerformTransition(roleCode, ProjectStatus.Cotizado, ProjectStatus.Finalizado, isReactivationEndpoint: false)
            .Should()
            .BeFalse();
    }

    [Theory]
    [InlineData("DIRECTOR", true)]
    [InlineData("PRESUPUESTADOR", false)]
    [InlineData("AUDITOR", false)]
    [InlineData("ADMINISTRADOR", false)]
    public void Solo_el_director_inhabilita(string roleCode, bool expected)
    {
        // SC-PROYECTOS-39.
        ProjectStatusTransitionPolicy
            .CanRolePerformTransition(roleCode, ProjectStatus.Cotizado, ProjectStatus.Inhabilitado, isReactivationEndpoint: false)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData("DIRECTOR", true)]
    [InlineData("PRESUPUESTADOR", false)]
    [InlineData("AUDITOR", false)]
    public void Solo_el_director_reactiva(string roleCode, bool expected)
    {
        ProjectStatusTransitionPolicy
            .CanRolePerformTransition(roleCode, ProjectStatus.Inhabilitado, ProjectStatus.FaltaInformacion, isReactivationEndpoint: true)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData("DIRECTOR", true)]
    [InlineData("PRESUPUESTADOR", true)]
    [InlineData("AUDITOR", false)]
    public void Director_y_presupuestador_retroceden_de_cotizado(string roleCode, bool expected)
    {
        // SC-PROYECTOS-40 y SC-PROYECTOS-41.
        ProjectStatusTransitionPolicy
            .CanRolePerformTransition(roleCode, ProjectStatus.Cotizado, ProjectStatus.FaltaInformacion, isReactivationEndpoint: false)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData("DIRECTOR", true)]
    [InlineData("PRESUPUESTADOR", false)]
    [InlineData("AUDITOR", false)]
    [InlineData("ADMINISTRADOR", false)]
    public void Solo_el_director_reabre_un_finalizado(string roleCode, bool expected)
    {
        // SC-PROYECTOS-44 y SC-PROYECTOS-45.
        ProjectStatusTransitionPolicy
            .CanRolePerformTransition(roleCode, ProjectStatus.Finalizado, ProjectStatus.FaltaInformacion, isReactivationEndpoint: false)
            .Should()
            .Be(expected);
    }

    [Theory]
    [InlineData("DIRECTOR", ProjectStatus.PorCotizar, ProjectStatus.FaltaInformacion, true)]
    [InlineData("PRESUPUESTADOR", ProjectStatus.PorCotizar, ProjectStatus.FaltaInformacion, true)]
    [InlineData("PRESUPUESTADOR", ProjectStatus.FaltaInformacion, ProjectStatus.Cotizado, true)]
    [InlineData("AUDITOR", ProjectStatus.PorCotizar, ProjectStatus.FaltaInformacion, false)]
    [InlineData("ADMINISTRADOR", ProjectStatus.PorCotizar, ProjectStatus.FaltaInformacion, false)]
    public void Alternan_falta_informacion_y_cotizado(
        string roleCode,
        ProjectStatus current,
        ProjectStatus target,
        bool expected)
    {
        ProjectStatusTransitionPolicy
            .CanRolePerformTransition(roleCode, current, target, isReactivationEndpoint: false)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void Parse_y_serializacion_del_estado_cubren_los_cinco_valores()
    {
        // SC-PROYECTOS-06.
        foreach (var status in Enum.GetValues<ProjectStatus>())
        {
            var code = ProjectStatusTransitionPolicy.ToStatusCode(status);
            ProjectStatusTransitionPolicy.TryParseStatusCode(code, out var parsed).Should().BeTrue();
            parsed.Should().Be(status);
        }
    }

    [Theory]
    [InlineData("APROBADO")]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_rechaza_un_estado_fuera_del_conjunto(string? code)
    {
        // SC-PROYECTOS-07.
        ProjectStatusTransitionPolicy.TryParseStatusCode(code, out _).Should().BeFalse();
    }
}
