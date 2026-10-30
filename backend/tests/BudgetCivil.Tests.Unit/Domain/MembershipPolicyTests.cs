using BudgetCivil.Domain.Memberships;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Domain;

public sealed class MembershipPolicyTests
{
    [Theory]
    [InlineData("DIRECTOR")]
    [InlineData("director ")]
    [InlineData("PRESUPUESTADOR")]
    [InlineData("AUDITOR")]
    public void CompanyMembershipPolicy_acepta_los_roles_asignables(string roleCode)
    {
        FluentActions.Invoking(() => CompanyMembershipPolicy.EnsureRoleAllowedForCompanyAssignment(roleCode))
            .Should().NotThrow();
    }

    [Theory]
    [InlineData("ADMINISTRADOR")]
    [InlineData("OTRO")]
    public void CompanyMembershipPolicy_rechaza_roles_no_asignables(string roleCode)
    {
        // SC-IDENTIDAD-30: el Administrador no pertenece a ninguna compañía.
        FluentActions.Invoking(() => CompanyMembershipPolicy.EnsureRoleAllowedForCompanyAssignment(roleCode))
            .Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("DIRECTOR")]
    [InlineData("PRESUPUESTADOR")]
    [InlineData("AUDITOR")]
    public void CompanyMembershipPolicy_permite_que_el_usuario_pertenezca_a_una_compania(string roleCode)
    {
        FluentActions.Invoking(() => CompanyMembershipPolicy.EnsureTargetCanBelongToCompany(roleCode))
            .Should().NotThrow();
    }

    [Fact]
    public void CompanyMembershipPolicy_trae_la_asignacion_solo_al_administrador_y_al_director_de_la_compania()
    {
        var companyId = Guid.NewGuid();

        FluentActions.Invoking(() => CompanyMembershipPolicy.EnsureActorCanAssign(companyId, "ADMINISTRADOR", null))
            .Should().NotThrow();
        FluentActions.Invoking(() => CompanyMembershipPolicy.EnsureActorCanAssign(companyId, "DIRECTOR", companyId))
            .Should().NotThrow();

        // SC-IDENTIDAD-29 y SC-IDENTIDAD-31: un Director no asigna fuera de su compañía.
        FluentActions.Invoking(() => CompanyMembershipPolicy.EnsureActorCanAssign(companyId, "DIRECTOR", Guid.NewGuid()))
            .Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => CompanyMembershipPolicy.EnsureActorCanAssign(companyId, "PRESUPUESTADOR", companyId))
            .Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => CompanyMembershipPolicy.EnsureActorCanDeactivate(companyId, "DIRECTOR", Guid.NewGuid()))
            .Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("DIRECTOR", true)]
    [InlineData("PRESUPUESTADOR", true)]
    [InlineData("AUDITOR", true)]
    [InlineData("ADMINISTRADOR", false)]
    public void CompanyMembershipPolicy_solo_los_roles_de_compania_tienen_una_sola_compania(string roleCode, bool expected)
    {
        CompanyMembershipPolicy.RequiresSingleActiveCompanyConstraint(roleCode).Should().Be(expected);
    }

    [Theory]
    [InlineData("DIRECTOR")]
    [InlineData("PRESUPUESTADOR")]
    [InlineData("AUDITOR")]
    public void ProjectMembershipPolicy_acepta_los_roles_asignables(string roleCode)
    {
        FluentActions.Invoking(() => ProjectMembershipPolicy.EnsureRoleAllowedForProjectAssignment(roleCode))
            .Should().NotThrow();
    }

    [Fact]
    public void ProjectMembershipPolicy_rechaza_al_administrador_y_a_un_rol_desconocido()
    {
        FluentActions.Invoking(() => ProjectMembershipPolicy.EnsureRoleAllowedForProjectAssignment("ADMINISTRADOR"))
            .Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => ProjectMembershipPolicy.EnsureRoleAllowedForProjectAssignment("OTRO"))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ProjectMembershipPolicy_el_director_asigna_usuarios_de_su_compania()
    {
        // SC-PROYECTOS-29 y SC-PROYECTOS-33.
        var companyId = Guid.NewGuid();

        FluentActions.Invoking(() =>
                ProjectMembershipPolicy.EnsureActorCanAssign(companyId, "DIRECTOR", companyId, "PRESUPUESTADOR"))
            .Should().NotThrow();

        FluentActions.Invoking(() =>
                ProjectMembershipPolicy.EnsureActorCanAssign(companyId, "DIRECTOR", Guid.NewGuid(), "PRESUPUESTADOR"))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ProjectMembershipPolicy_el_presupuestador_solo_asigna_presupuestadores_y_auditores()
    {
        // SC-PROYECTOS-30 y SC-PROYECTOS-31.
        var companyId = Guid.NewGuid();

        FluentActions.Invoking(() =>
                ProjectMembershipPolicy.EnsureActorCanAssign(companyId, "PRESUPUESTADOR", companyId, "AUDITOR"))
            .Should().NotThrow();

        FluentActions.Invoking(() =>
                ProjectMembershipPolicy.EnsureActorCanAssign(companyId, "PRESUPUESTADOR", companyId, "DIRECTOR"))
            .Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("AUDITOR")]
    [InlineData("ADMINISTRADOR")]
    public void ProjectMembershipPolicy_un_rol_sin_alcance_no_asigna(string roleCode)
    {
        // SC-PROYECTOS-32.
        var companyId = Guid.NewGuid();

        FluentActions.Invoking(() =>
                ProjectMembershipPolicy.EnsureActorCanAssign(companyId, roleCode, companyId, "PRESUPUESTADOR"))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ProjectMembershipPolicy_valida_la_baja_por_rol_y_compania()
    {
        var companyId = Guid.NewGuid();

        FluentActions.Invoking(() =>
                ProjectMembershipPolicy.EnsureActorCanDeactivate(companyId, "DIRECTOR", companyId, "PRESUPUESTADOR"))
            .Should().NotThrow();
        FluentActions.Invoking(() =>
                ProjectMembershipPolicy.EnsureActorCanDeactivate(companyId, "PRESUPUESTADOR", companyId, "AUDITOR"))
            .Should().NotThrow();
        FluentActions.Invoking(() =>
                ProjectMembershipPolicy.EnsureActorCanDeactivate(companyId, "PRESUPUESTADOR", companyId, "DIRECTOR"))
            .Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() =>
                ProjectMembershipPolicy.EnsureActorCanDeactivate(companyId, "AUDITOR", companyId, "AUDITOR"))
            .Should().Throw<InvalidOperationException>();
    }
}
