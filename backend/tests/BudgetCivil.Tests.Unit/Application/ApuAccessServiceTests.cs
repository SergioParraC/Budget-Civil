using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-U-23`: alcance por rol en lectura y escritura de APUs y Matrices (`REQ-APUS-16`, `REQ-APUS-17`,
/// `SC-APUS-71`…`83`, `91`).
/// </summary>
public sealed class ApuAccessServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    [Theory]
    [InlineData("ADMINISTRADOR", false)]
    [InlineData("DIRECTOR", false)]
    [InlineData("DIRECTOR", true)]
    [InlineData("PRESUPUESTADOR", false)]
    [InlineData("AUDITOR", true)]
    public void La_lectura_permitida_por_rol_no_lanza_excepcion(string role, bool isMember)
    {
        var actor = Actor(role, CompanyId);
        var access = new ApuProjectAccess(ProjectId, CompanyId, isMember);

        var action = () => ApuAccessService.EnsureCanRead(actor, access);

        action.Should().NotThrow();
    }

    [Fact]
    public void El_auditor_sin_pertenencia_no_lee()
    {
        // SC-APUS-80.
        var action = () => ApuAccessService.EnsureCanRead(
            Actor("AUDITOR", CompanyId),
            new ApuProjectAccess(ProjectId, CompanyId, IsMember: false));

        action.Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be(ApuRules.ApuReadForbidden);
    }

    [Theory]
    [InlineData("DIRECTOR")]
    [InlineData("PRESUPUESTADOR")]
    [InlineData("AUDITOR")]
    [InlineData("ADMINISTRADOR")]
    public void Nadie_lee_apus_de_otra_compania(string role)
    {
        // SC-APUS-81 y SC-APUS-82.
        var action = () => ApuAccessService.EnsureCanRead(
            Actor(role, Guid.NewGuid()),
            new ApuProjectAccess(ProjectId, CompanyId, IsMember: true));

        if (role == "ADMINISTRADOR")
        {
            action.Should().NotThrow();
            return;
        }

        action.Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be(ApuRules.ApuReadForbidden);
    }

    [Theory]
    [InlineData("DIRECTOR")]
    [InlineData("PRESUPUESTADOR")]
    public void El_director_y_el_presupuestador_con_pertenencia_escriben(string role)
    {
        // SC-APUS-71 y SC-APUS-72.
        var action = () => ApuAccessService.EnsureCanWrite(
            Actor(role, CompanyId),
            new ApuProjectAccess(ProjectId, CompanyId, IsMember: true));

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("DIRECTOR")]
    [InlineData("PRESUPUESTADOR")]
    public void La_escritura_sin_pertenencia_se_deniega(string role)
    {
        // SC-APUS-73 y SC-APUS-91.
        var action = () => ApuAccessService.EnsureCanWrite(
            Actor(role, CompanyId),
            new ApuProjectAccess(ProjectId, CompanyId, IsMember: false));

        action.Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be(ApuRules.ApuWriteForbidden);
    }

    [Theory]
    [InlineData("ADMINISTRADOR")]
    [InlineData("AUDITOR")]
    public void El_administrador_y_el_auditor_no_escriben(string role)
    {
        // SC-APUS-74 y SC-APUS-75.
        var action = () => ApuAccessService.EnsureCanWrite(
            Actor(role, CompanyId),
            new ApuProjectAccess(ProjectId, CompanyId, IsMember: true));

        action.Should().Throw<ForbiddenAppException>()
            .Which.Code.Should().Be(ApuRules.ApuWriteForbidden);
    }

    private static ActorAccessSnapshot Actor(string role, Guid? companyId)
    {
        return new ActorAccessSnapshot(
            Guid.NewGuid(),
            companyId,
            Guid.NewGuid(),
            role,
            AccountEnabled: true,
            AccountLocked: false,
            RoleIsActive: true,
            CompanyIsActive: true,
            GrantedPermissionKeys: []);
    }
}
