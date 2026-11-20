using BudgetCivil.Application.Calculation.Repositories;
using BudgetCivil.Application.Calculation.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-27`: alcance por rol del presupuesto (`REQ-CALCULO-04`, `SC-CALCULO-07`, `13`…`18`).</summary>
public sealed class CalculationAccessServiceTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    [Theory]
    [InlineData("ADMINISTRADOR", false, false, false)]
    [InlineData("DIRECTOR", true, true, false)]
    [InlineData("PRESUPUESTADOR", true, true, false)]
    [InlineData("AUDITOR", true, true, false)]
    [InlineData("PRESUPUESTADOR", true, false, true)]
    [InlineData("AUDITOR", true, false, true)]
    [InlineData("AUDITOR", false, true, true)]
    public async Task El_alcance_de_lectura_sigue_la_matriz_del_arbol(
        string roleCode,
        bool sameCompany,
        bool isMember,
        bool forbidden)
    {
        var service = AccessService(roleCode, sameCompany, isMember);

        var action = () => service.EnsureReadAsync(ActorId, ProjectId);

        if (forbidden)
        {
            var exception = await action.Should().ThrowAsync<ForbiddenAppException>();
            exception.Which.Code.Should().Be("WBS_READ_FORBIDDEN");
        }
        else
        {
            await action.Should().NotThrowAsync();
        }
    }

    [Fact]
    public async Task Un_proyecto_inexistente_responde_404()
    {
        // SC-CALCULO-08.
        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Actor("DIRECTOR", sameCompany: true));

        var accessReader = new Mock<ICalculationProjectAccessReader>();
        accessReader
            .Setup(reader => reader.GetAccessAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CalculationProjectAccess?)null);

        var service = new CalculationAccessService(actorReader.Object, accessReader.Object);

        var action = () => service.EnsureReadAsync(ActorId, ProjectId);

        var exception = await action.Should().ThrowAsync<NotFoundAppException>();
        exception.Which.Code.Should().Be("PROJECT_NOT_FOUND");
    }

    private static CalculationAccessService AccessService(
        string roleCode,
        bool sameCompany,
        bool isMember)
    {
        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Actor(roleCode, sameCompany));

        var accessReader = new Mock<ICalculationProjectAccessReader>();
        accessReader
            .Setup(reader => reader.GetAccessAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CalculationProjectAccess(ProjectId, CompanyId, isMember));

        return new CalculationAccessService(actorReader.Object, accessReader.Object);
    }

    private static ActorAccessSnapshot Actor(string roleCode, bool sameCompany)
    {
        return new ActorAccessSnapshot(
            ActorId,
            sameCompany ? CompanyId : Guid.NewGuid(),
            Guid.NewGuid(),
            roleCode,
            AccountEnabled: true,
            AccountLocked: false,
            RoleIsActive: true,
            CompanyIsActive: true,
            GrantedPermissionKeys: []);
    }
}
