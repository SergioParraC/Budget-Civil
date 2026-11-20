using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Projects.Commands.ChangeProjectStatus;
using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-29`: precondición de `REQ-PROYECTOS-05` en la transición a `COTIZADO` (`D-13`).</summary>
public sealed class ProjectQuotationPreconditionTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    [Fact]
    public async Task La_transicion_a_cotizado_falla_con_409_y_el_conteo_pendiente()
    {
        // SC-PROYECTOS-10.
        var project = Project();
        var projects = Projects(project);
        var transitions = new Mock<IProjectTransitionRepository>();

        var handler = new ChangeProjectStatusCommandHandler(
            projects.Object,
            transitions.Object,
            Pending(3).Object,
            new Mock<ISecurityAuditService>().Object);

        var action = () => handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new ChangeProjectStatusCommand { TargetStatus = "COTIZADO" });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be("PROJECT_QUOTATION_INCOMPLETE");
        exception.Which.Extensions!["pendingResources"].Should().Be(3);
        project.Status.Should().Be(ProjectStatus.FaltaInformacion);
        transitions.Verify(
            repo => repo.AddStatusTransitionAsync(
                It.IsAny<ProjectStatusTransition>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        projects.Verify(
            repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Sin_insumos_pendientes_la_transicion_a_cotizado_se_aplica()
    {
        // SC-PROYECTOS-11.
        var project = Project();
        var projects = Projects(project);
        var transitions = new Mock<IProjectTransitionRepository>();
        var audit = new Mock<ISecurityAuditService>();

        var handler = new ChangeProjectStatusCommandHandler(
            projects.Object,
            transitions.Object,
            Pending(0).Object,
            audit.Object);

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new ChangeProjectStatusCommand { TargetStatus = "COTIZADO" });

        response.NewStatus.Should().Be("COTIZADO");
        project.Status.Should().Be(ProjectStatus.Cotizado);
        transitions.Verify(
            repo => repo.AddStatusTransitionAsync(
                It.Is<ProjectStatusTransition>(transition =>
                    transition.NewStatus == ProjectStatus.Cotizado),
                It.IsAny<CancellationToken>()),
            Times.Once);
        audit.Verify(
            service => service.WriteAsync(
                It.IsAny<SecurityAuditWriteRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Project Project() => new()
    {
        Id = ProjectId,
        CompanyId = CompanyId,
        Status = ProjectStatus.FaltaInformacion
    };

    private static Mock<IProjectRepository> Projects(Project project)
    {
        var projects = new Mock<IProjectRepository>();
        projects
            .Setup(repo => repo.GetActorByIdAsync(ActorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Actor());
        projects
            .Setup(repo => repo.GetProjectByIdAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);
        projects
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return projects;
    }

    private static UserAccount Actor()
    {
        var actor = new UserAccount
        {
            Id = ActorId,
            AccountEnabled = true,
            AccountLocked = false,
            Role = new Role { Id = Guid.NewGuid(), Code = "DIRECTOR", IsActive = true }
        };

        actor.Memberships.Add(new CompanyMembership
        {
            Id = Guid.NewGuid(),
            UserId = ActorId,
            CompanyId = CompanyId,
            AccountEnabled = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        return actor;
    }

    private static Mock<IProjectQuotationStatusReader> Pending(int pending)
    {
        var reader = new Mock<IProjectQuotationStatusReader>();
        reader
            .Setup(instance => instance.CountPendingResourcesAsync(
                ProjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        return reader;
    }
}
