using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

public sealed class ResourceAccessServiceTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid OtherCompanyId = Guid.NewGuid();

    [Fact]
    public async Task El_administrador_puede_leer_cualquier_proyecto()
    {
        var service = CreateService(Actor("ADMINISTRADOR", companyId: null), Access(CompanyId, isMember: false));

        var action = () => service.EnsureReadAsync(Guid.NewGuid(), ProjectId);

        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task El_administrador_no_puede_escribir()
    {
        var service = CreateService(Actor("ADMINISTRADOR", companyId: null), Access(CompanyId, isMember: false));

        var action = () => service.EnsureWriteAsync(Guid.NewGuid(), ProjectId);

        var exception = await action.Should().ThrowAsync<ForbiddenAppException>();
        exception.Which.Code.Should().Be("RESOURCE_WRITE_FORBIDDEN");
    }

    [Fact]
    public async Task El_director_lee_y_escribe_en_su_compania()
    {
        var service = CreateService(Actor("DIRECTOR", CompanyId), Access(CompanyId, isMember: false));

        await service.EnsureReadAsync(Guid.NewGuid(), ProjectId);
        await service.EnsureWriteAsync(Guid.NewGuid(), ProjectId);
    }

    [Fact]
    public async Task El_director_no_accede_a_otra_compania()
    {
        var service = CreateService(Actor("DIRECTOR", CompanyId), Access(OtherCompanyId, isMember: false));

        var action = () => service.EnsureReadAsync(Guid.NewGuid(), ProjectId);

        var exception = await action.Should().ThrowAsync<ForbiddenAppException>();
        exception.Which.Code.Should().Be("RESOURCE_READ_FORBIDDEN");
    }

    [Fact]
    public async Task El_presupuestador_asignado_lee_y_escribe()
    {
        var service = CreateService(Actor("PRESUPUESTADOR", CompanyId), Access(CompanyId, isMember: true));

        await service.EnsureReadAsync(Guid.NewGuid(), ProjectId);
        await service.EnsureWriteAsync(Guid.NewGuid(), ProjectId);
    }

    [Fact]
    public async Task El_presupuestador_no_asignado_no_escribe()
    {
        var service = CreateService(Actor("PRESUPUESTADOR", CompanyId), Access(CompanyId, isMember: false));

        var action = () => service.EnsureWriteAsync(Guid.NewGuid(), ProjectId);

        var exception = await action.Should().ThrowAsync<ForbiddenAppException>();
        exception.Which.Code.Should().Be("RESOURCE_WRITE_FORBIDDEN");
    }

    [Fact]
    public async Task El_auditor_asignado_lee_pero_no_escribe()
    {
        var service = CreateService(Actor("AUDITOR", CompanyId), Access(CompanyId, isMember: true));

        await service.EnsureReadAsync(Guid.NewGuid(), ProjectId);

        var action = () => service.EnsureWriteAsync(Guid.NewGuid(), ProjectId);
        var exception = await action.Should().ThrowAsync<ForbiddenAppException>();
        exception.Which.Code.Should().Be("RESOURCE_WRITE_FORBIDDEN");
    }

    [Fact]
    public async Task El_auditor_no_asignado_no_lee()
    {
        var service = CreateService(Actor("AUDITOR", CompanyId), Access(CompanyId, isMember: false));

        var action = () => service.EnsureReadAsync(Guid.NewGuid(), ProjectId);

        var exception = await action.Should().ThrowAsync<ForbiddenAppException>();
        exception.Which.Code.Should().Be("RESOURCE_READ_FORBIDDEN");
    }

    [Fact]
    public async Task Un_actor_deshabilitado_no_opera()
    {
        var service = CreateService(Actor("DIRECTOR", CompanyId, enabled: false), Access(CompanyId, isMember: false));

        var action = () => service.EnsureReadAsync(Guid.NewGuid(), ProjectId);

        var exception = await action.Should().ThrowAsync<ForbiddenAppException>();
        exception.Which.Code.Should().Be("ACTOR_NOT_ENABLED");
    }

    [Fact]
    public async Task Un_proyecto_inexistente_devuelve_no_encontrado()
    {
        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Actor("DIRECTOR", CompanyId));

        var projectReader = new Mock<IResourceProjectAccessReader>();
        projectReader
            .Setup(reader => reader.GetAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceProjectAccess?)null);

        var service = new ResourceAccessService(actorReader.Object, projectReader.Object);

        var action = () => service.EnsureReadAsync(Guid.NewGuid(), ProjectId);

        var exception = await action.Should().ThrowAsync<NotFoundAppException>();
        exception.Which.Code.Should().Be("PROJECT_NOT_FOUND");
    }

    private static ResourceAccessService CreateService(
        ActorAccessSnapshot actor,
        ResourceProjectAccess access)
    {
        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(actor);

        var projectReader = new Mock<IResourceProjectAccessReader>();
        projectReader
            .Setup(reader => reader.GetAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(access);

        return new ResourceAccessService(actorReader.Object, projectReader.Object);
    }

    private static ActorAccessSnapshot Actor(string roleCode, Guid? companyId, bool enabled = true)
    {
        return new ActorAccessSnapshot(
            Guid.NewGuid(),
            companyId,
            Guid.NewGuid(),
            roleCode,
            enabled,
            AccountLocked: false,
            RoleIsActive: true,
            CompanyIsActive: companyId.HasValue ? true : null,
            GrantedPermissionKeys: []);
    }

    private static ResourceProjectAccess Access(Guid companyId, bool isMember)
    {
        return new ResourceProjectAccess(ProjectId, companyId, isMember);
    }
}
