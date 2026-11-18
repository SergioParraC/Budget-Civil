using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-U-18`: alcance del módulo `M-03` (`SC-EDT-42`…`50`, `SC-EDT-76`, `REQ-EDT-13` y `REQ-EDT-14`).
/// </summary>
public sealed class WbsAccessServiceTests
{
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid OtherCompanyId = Guid.NewGuid();

    [Fact]
    public async Task El_administrador_lee_cualquier_proyecto()
    {
        var service = CreateService("ADMINISTRADOR", actorCompanyId: OtherCompanyId, isMember: false);

        var (_, access) = await service.EnsureReadAsync(ActorId, ProjectId);

        access.ProjectId.Should().Be(ProjectId);
    }

    [Fact]
    public async Task El_director_lee_en_su_compania_y_solo_escribe_con_pertenencia()
    {
        var reader = CreateService("DIRECTOR", CompanyId, isMember: false);
        var member = CreateService("DIRECTOR", CompanyId, isMember: true);

        await reader.Invoking(instance => instance.EnsureReadAsync(ActorId, ProjectId))
            .Should().NotThrowAsync();
        await reader.Invoking(instance => instance.EnsureWriteAsync(ActorId, ProjectId))
            .Should().ThrowAsync<ForbiddenAppException>();
        await member.Invoking(instance => instance.EnsureWriteAsync(ActorId, ProjectId))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task El_director_de_otra_compania_no_lee_ni_escribe()
    {
        var service = CreateService("DIRECTOR", OtherCompanyId, isMember: false);

        var read = await service.Invoking(instance => instance.EnsureReadAsync(ActorId, ProjectId))
            .Should().ThrowAsync<ForbiddenAppException>();
        read.Which.Code.Should().Be("WBS_READ_FORBIDDEN");

        var write = await service.Invoking(instance => instance.EnsureWriteAsync(ActorId, ProjectId))
            .Should().ThrowAsync<ForbiddenAppException>();
        write.Which.Code.Should().Be("WBS_WRITE_FORBIDDEN");
    }

    [Fact]
    public async Task El_presupuestador_asignado_escribe_y_el_no_asignado_no()
    {
        var assigned = CreateService("PRESUPUESTADOR", CompanyId, isMember: true);
        await assigned.Invoking(instance => instance.EnsureWriteAsync(ActorId, ProjectId))
            .Should().NotThrowAsync();

        var notAssigned = CreateService("PRESUPUESTADOR", CompanyId, isMember: false);
        var exception = await notAssigned.Invoking(instance => instance.EnsureWriteAsync(ActorId, ProjectId))
            .Should().ThrowAsync<ForbiddenAppException>();
        exception.Which.Code.Should().Be("WBS_WRITE_FORBIDDEN");
    }

    [Fact]
    public async Task El_auditor_lee_pero_no_escribe_ni_consulta_la_actividad()
    {
        var service = CreateService("AUDITOR", CompanyId, isMember: true);

        await service.Invoking(instance => instance.EnsureReadAsync(ActorId, ProjectId))
            .Should().NotThrowAsync();

        var write = await service.Invoking(instance => instance.EnsureWriteAsync(ActorId, ProjectId))
            .Should().ThrowAsync<ForbiddenAppException>();
        write.Which.Code.Should().Be("WBS_WRITE_FORBIDDEN");

        var activity = await service.Invoking(instance => instance.EnsureActivityReadAsync(ActorId, ProjectId))
            .Should().ThrowAsync<ForbiddenAppException>();
        activity.Which.Code.Should().Be("WBS_READ_FORBIDDEN");
    }

    [Fact]
    public async Task El_administrador_no_escribe()
    {
        var service = CreateService("ADMINISTRADOR", OtherCompanyId, isMember: false);

        var exception = await service.Invoking(instance => instance.EnsureWriteAsync(ActorId, ProjectId))
            .Should().ThrowAsync<ForbiddenAppException>();
        exception.Which.Code.Should().Be("WBS_WRITE_FORBIDDEN");
    }

    [Fact]
    public async Task Un_proyecto_inexistente_responde_project_not_found()
    {
        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Actor("DIRECTOR", CompanyId));

        var projectReader = new Mock<IWbsProjectAccessReader>();
        projectReader
            .Setup(reader => reader.GetAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WbsProjectAccess?)null);

        var service = new WbsAccessService(actorReader.Object, projectReader.Object);

        var exception = await service.Invoking(instance => instance.EnsureReadAsync(ActorId, ProjectId))
            .Should().ThrowAsync<NotFoundAppException>();
        exception.Which.Code.Should().Be("PROJECT_NOT_FOUND");
    }

    private static WbsAccessService CreateService(string roleCode, Guid actorCompanyId, bool isMember)
    {
        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Actor(roleCode, actorCompanyId));

        var projectReader = new Mock<IWbsProjectAccessReader>();
        projectReader
            .Setup(reader => reader.GetAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WbsProjectAccess(ProjectId, CompanyId, isMember));

        return new WbsAccessService(actorReader.Object, projectReader.Object);
    }

    private static ActorAccessSnapshot Actor(string roleCode, Guid companyId) => new(
        ActorId,
        companyId,
        Guid.NewGuid(),
        roleCode,
        AccountEnabled: true,
        AccountLocked: false,
        RoleIsActive: true,
        CompanyIsActive: true,
        GrantedPermissionKeys: []);
}
