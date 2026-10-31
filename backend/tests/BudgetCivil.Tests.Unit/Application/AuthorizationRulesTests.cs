using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Companies.UseCases;
using BudgetCivil.Application.Users.Administration.UseCases;
using BudgetCivil.Tests.Unit.TestData;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

public sealed class AuthorizationRulesTests
{
    [Fact]
    public void Solo_el_administrador_crea_usuarios()
    {
        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanCreateUsers(
                TestUsers.WithRole(TestUsers.Administrator)))
            .Should().NotThrow();

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanCreateUsers(
                TestUsers.WithRole(TestUsers.Director, Guid.NewGuid())))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("ADMIN_ONLY_USER_CREATION");
    }

    [Fact]
    public void El_actor_debe_estar_habilitado()
    {
        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanCreateUsers(
                TestUsers.WithRole(TestUsers.Administrator, accountEnabled: false)))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("ACTOR_NOT_ENABLED");

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanCreateUsers(null))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("AUTH_USER_NOT_FOUND");
    }

    [Fact]
    public void El_administrador_gestiona_cualquier_usuario_y_el_director_solo_su_compania()
    {
        var companyId = Guid.NewGuid();

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanManageTarget(
                TestUsers.WithRole(TestUsers.Administrator),
                TestUsers.WithRole(TestUsers.Estimator, Guid.NewGuid())))
            .Should().NotThrow();

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanManageTarget(
                TestUsers.WithRole(TestUsers.Director, companyId),
                TestUsers.WithRole(TestUsers.Estimator, companyId)))
            .Should().NotThrow();

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanManageTarget(
                TestUsers.WithRole(TestUsers.Director, companyId),
                TestUsers.WithRole(TestUsers.Estimator, Guid.NewGuid())))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("TARGET_USER_OUT_OF_SCOPE");

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanManageTarget(
                TestUsers.WithRole(TestUsers.Estimator, companyId),
                TestUsers.WithRole(TestUsers.Estimator, companyId)))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("TARGET_USER_OUT_OF_SCOPE");

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanManageTarget(
                TestUsers.WithRole(TestUsers.Administrator),
                null))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("TARGET_USER_NOT_FOUND");
    }

    [Fact]
    public void La_asignacion_de_compania_respeta_el_alcance_del_director()
    {
        var companyId = Guid.NewGuid();

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanAssignCompany(
                TestUsers.WithRole(TestUsers.Administrator),
                TestUsers.WithRole(TestUsers.Estimator),
                companyId))
            .Should().NotThrow();

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanAssignCompany(
                TestUsers.WithRole(TestUsers.Director, companyId),
                TestUsers.WithRole(TestUsers.Estimator),
                companyId))
            .Should().NotThrow();

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanAssignCompany(
                TestUsers.WithRole(TestUsers.Director, companyId),
                TestUsers.WithRole(TestUsers.Estimator),
                Guid.NewGuid()))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("MANAGER_COMPANY_SCOPE_VIOLATION");

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanAssignCompany(
                TestUsers.WithRole(TestUsers.Estimator, companyId),
                TestUsers.WithRole(TestUsers.Estimator),
                companyId))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("COMPANY_ASSIGNMENT_FORBIDDEN");

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanAssignCompany(
                TestUsers.WithRole(TestUsers.Director),
                TestUsers.WithRole(TestUsers.Estimator),
                companyId))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("MANAGER_COMPANY_CONTEXT_REQUIRED");

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureActorCanAssignCompany(
                TestUsers.WithRole(TestUsers.Director, companyId),
                TestUsers.WithRole(TestUsers.Estimator, Guid.NewGuid()),
                companyId))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("TARGET_USER_OUT_OF_SCOPE");
    }

    [Fact]
    public void El_director_no_eleva_a_administrador()
    {
        var director = TestUsers.WithRole(TestUsers.Director, Guid.NewGuid());

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureManagerCannotElevateToAdministrator(
                director,
                TestUsers.Administrator))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("MANAGER_ROLE_ELEVATION_FORBIDDEN");

        FluentActions.Invoking(() => AdministrationAuthorizationRules.EnsureManagerCannotElevateToAdministrator(
                director,
                TestUsers.Estimator))
            .Should().NotThrow();
    }

    [Fact]
    public void Solo_el_administrador_y_el_director_crean_companias()
    {
        FluentActions.Invoking(() => CompanyAdministrationAuthorizationRules.EnsureActorCanCreateCompany(
                TestUsers.WithRole(TestUsers.Administrator)))
            .Should().NotThrow();

        FluentActions.Invoking(() => CompanyAdministrationAuthorizationRules.EnsureActorCanCreateCompany(
                TestUsers.WithRole(TestUsers.Director)))
            .Should().NotThrow();

        FluentActions.Invoking(() => CompanyAdministrationAuthorizationRules.EnsureActorCanCreateCompany(
                TestUsers.WithRole(TestUsers.Estimator)))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("COMPANY_CREATE_FORBIDDEN");
    }

    [Fact]
    public void El_director_edita_solo_su_compania()
    {
        var companyId = Guid.NewGuid();

        FluentActions.Invoking(() => CompanyAdministrationAuthorizationRules.EnsureActorCanUpdateCompany(
                TestUsers.WithRole(TestUsers.Director, companyId),
                companyId))
            .Should().NotThrow();

        FluentActions.Invoking(() => CompanyAdministrationAuthorizationRules.EnsureActorCanUpdateCompany(
                TestUsers.WithRole(TestUsers.Director, companyId),
                Guid.NewGuid()))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("COMPANY_UPDATE_FORBIDDEN");

        FluentActions.Invoking(() => CompanyAdministrationAuthorizationRules.EnsureActorCanUpdateCompany(
                TestUsers.WithRole(TestUsers.Administrator),
                companyId))
            .Should().NotThrow();
    }

    [Fact]
    public void Inhabilitar_reactivar_y_listar_companias_es_solo_del_administrador()
    {
        FluentActions.Invoking(() => CompanyAdministrationAuthorizationRules.EnsureActorCanDeactivateCompany(
                TestUsers.WithRole(TestUsers.Administrator)))
            .Should().NotThrow();

        FluentActions.Invoking(() => CompanyAdministrationAuthorizationRules.EnsureActorCanDeactivateCompany(
                TestUsers.WithRole(TestUsers.Director, Guid.NewGuid())))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("COMPANY_DEACTIVATE_FORBIDDEN");

        FluentActions.Invoking(() => CompanyAdministrationAuthorizationRules.EnsureActorCanReactivateCompany(
                TestUsers.WithRole(TestUsers.Administrator)))
            .Should().NotThrow();

        FluentActions.Invoking(() => CompanyAdministrationAuthorizationRules.EnsureActorCanReadCompanies(
                TestUsers.WithRole(TestUsers.Director)))
            .Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be("COMPANY_READ_FORBIDDEN");
    }
}
