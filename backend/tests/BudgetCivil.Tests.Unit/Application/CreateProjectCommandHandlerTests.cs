using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Projects.Commands.CreateProject;
using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `INC-20`: el alta de proyecto debe registrar en `project_members` al Director responsable y al
/// Presupuestador creador, porque la escritura del EDT y de los APUs exige pertenencia.
/// </summary>
public sealed class CreateProjectCommandHandlerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid DirectorId = Guid.NewGuid();
    private static readonly Guid EstimatorId = Guid.NewGuid();
    private static readonly Guid AdministratorId = Guid.NewGuid();

    [Fact]
    public async Task El_director_que_crea_queda_como_miembro_director_del_proyecto()
    {
        // SC-PROYECTOS-01.
        var director = Actor(DirectorId, "DIRECTOR");
        var repository = Repository(director, DirectorId);
        var members = CaptureMembers(repository);
        Project? createdProject = null;
        repository
            .Setup(instance => instance.AddProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((project, _) => createdProject = project)
            .Returns(Task.CompletedTask);

        var response = await CreateHandler(repository).ExecuteAsync(DirectorId, ValidCommand());

        response.ProjectId.Should().NotBe(Guid.Empty);
        createdProject!.DirectorOwnerUserId.Should().Be(DirectorId);
        members.Should().ContainSingle();
        members[0].ProjectId.Should().Be(response.ProjectId);
        members[0].UserId.Should().Be(DirectorId);
        members[0].RoleInProject.Should().Be("DIRECTOR");
        members[0].AssignedByUserId.Should().Be(DirectorId);
    }

    [Fact]
    public async Task El_presupuestador_creador_tambien_queda_como_miembro_del_proyecto()
    {
        // SC-PROYECTOS-04.
        var estimator = Actor(EstimatorId, "PRESUPUESTADOR");
        var repository = Repository(estimator, DirectorId);
        var members = CaptureMembers(repository);

        var response = await CreateHandler(repository).ExecuteAsync(EstimatorId, ValidCommand());

        members.Should().HaveCount(2);
        members.Should().Contain(member =>
            member.ProjectId == response.ProjectId &&
            member.UserId == DirectorId &&
            member.RoleInProject == "DIRECTOR");
        members.Should().Contain(member =>
            member.ProjectId == response.ProjectId &&
            member.UserId == EstimatorId &&
            member.RoleInProject == "PRESUPUESTADOR");
    }

    [Fact]
    public async Task El_administrador_no_queda_como_miembro_pero_si_el_director_responsable()
    {
        var administrator = Actor(AdministratorId, "ADMINISTRADOR");
        var repository = Repository(administrator, DirectorId);
        var members = CaptureMembers(repository);

        await CreateHandler(repository).ExecuteAsync(AdministratorId, ValidCommand());

        members.Should().ContainSingle();
        members[0].UserId.Should().Be(DirectorId);
        members[0].RoleInProject.Should().Be("DIRECTOR");
        members.Should().NotContain(member => member.UserId == AdministratorId);
    }

    private static CreateProjectCommandHandler CreateHandler(Mock<IProjectRepository> repository) =>
        new(
            repository.Object,
            new Mock<ISecurityAuditService>().Object,
            new CreateProjectCommandValidator());

    private static CreateProjectCommand ValidCommand() => new()
    {
        Name = "Proyecto de prueba",
        CountryId = Guid.NewGuid(),
        DepartmentId = Guid.NewGuid(),
        MunicipalityId = Guid.NewGuid(),
        BaseCurrencyId = Guid.NewGuid()
    };

    private static List<ProjectMember> CaptureMembers(Mock<IProjectRepository> repository)
    {
        var members = new List<ProjectMember>();
        repository
            .Setup(instance => instance.AddProjectMemberAsync(
                It.IsAny<ProjectMember>(),
                It.IsAny<CancellationToken>()))
            .Callback<ProjectMember, CancellationToken>((member, _) => members.Add(member))
            .Returns(Task.CompletedTask);

        return members;
    }

    private static Mock<IProjectRepository> Repository(UserAccount actor, Guid oldestDirectorUserId)
    {
        var repository = new Mock<IProjectRepository>();
        repository
            .Setup(instance => instance.GetActorByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actor);
        repository
            .Setup(instance => instance.GetCompanyByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Company { Id = CompanyId, IsActive = true });
        repository
            .Setup(instance => instance.AreProjectCatalogReferencesValidAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository
            .Setup(instance => instance.HasActiveDirectorInCompanyAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository
            .Setup(instance => instance.GetOldestActiveDirectorUserIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldestDirectorUserId);
        repository
            .Setup(instance => instance.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return repository;
    }

    private static UserAccount Actor(Guid userId, string roleCode)
    {
        var actor = new UserAccount
        {
            Id = userId,
            AccountEnabled = true,
            AccountLocked = false,
            Role = new Role { Id = Guid.NewGuid(), Code = roleCode, IsActive = true }
        };

        actor.Memberships.Add(new CompanyMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = CompanyId,
            AccountEnabled = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        return actor;
    }
}
