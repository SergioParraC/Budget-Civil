using BudgetCivil.Application.Calculation.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Pagination;
using BudgetCivil.Application.Apus.Commands.WbsItemApu;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Wbs.Commands.CreateWbsChapter;
using BudgetCivil.Application.Wbs.Commands.DeleteWbsChapter;
using BudgetCivil.Application.Wbs.Commands.DeleteWbsItem;
using BudgetCivil.Application.Wbs.Commands.MoveWbsNode;
using BudgetCivil.Application.Wbs.Commands.ReorderWbsNodes;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Queries;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Domain.Wbs;
using BudgetCivil.Infrastructure.Persistence;
using BudgetCivil.Infrastructure.Persistence.Repositories;
using BudgetCivil.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration;

/// <summary>
/// `T-I-40`: jerarquía, orden, búsqueda, movimiento, cascada, actividad y participantes con dos proyectos
/// de la misma compañía (`SC-EDT-04`, `05`, `30`…`36`, `51`…`76`).
/// </summary>
public sealed class WbsWorkflowTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task El_flujo_completo_del_edt_funciona_contra_sql_server()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var project = TestDataFactory.CreateProject(data, "Proyecto EDT");
        context.Projects.Add(project);
        context.ProjectMembers.Add(TestDataFactory.CreateProjectMember(data, project));
        await context.SaveChangesAsync();

        var accessService = new WbsAccessService(
            new ActorAccessReader(context),
            new WbsProjectAccessReader(context));
        var repository = new WbsRepository(context);
        var apuGateway = new WbsApuGateway(context);
        var apuRepository = new ApuRepository(context);

        var createChapter = new CreateWbsChapterCommandHandler(
            accessService,
            repository,
            new CreateWbsChapterCommandValidator());
        var createItem = new CreateWbsItemCommandHandler(
            accessService,
            repository,
            apuRepository,
            new ApuDetailService(apuRepository, apuGateway),
            new CreateWbsItemCommandValidator());
        var moveNode = new MoveWbsNodeCommandHandler(
            accessService,
            repository,
            new MoveWbsNodeCommandValidator());
        var reorder = new ReorderWbsNodesCommandHandler(
            accessService,
            repository,
            new ReorderWbsNodesCommandValidator());
        var deleteChapter = new DeleteWbsChapterCommandHandler(
            accessService,
            repository,
            apuGateway,
            new DeleteWbsChapterCommandValidator());
        var getTree = new GetWbsTreeQueryHandler(
            accessService,
            repository,
            apuGateway,
            new ApuCostService(new ProjectCostReader(context)));
        var deleteItem = new DeleteWbsItemCommandHandler(
            accessService,
            repository,
            apuGateway,
            new DeleteWbsItemCommandValidator());
        var search = new SearchWbsQueryHandler(
            accessService,
            repository,
            new ListingQueryPipeline(new PaginationValidator()));
        var activity = new GetWbsActivityQueryHandler(accessService, repository);

        var root = await createChapter.ExecuteAsync(
            data.Director.Id,
            project.Id,
            new CreateWbsChapterCommand { Code = "1", Name = "Capítulo raíz" });
        var child = await createChapter.ExecuteAsync(
            data.Director.Id,
            project.Id,
            new CreateWbsChapterCommand { Code = "1.1", Name = "Subcapítulo", ParentChapterId = root.Id });
        var item = await createItem.ExecuteAsync(
            data.Director.Id,
            project.Id,
            new CreateWbsItemCommand
            {
                Code = "CIM.1",
                QuantityWork = 12.5m,
                Apu = new CreateWbsItemApuDraft
                {
                    Description = "Actividad de prueba",
                    Unit = "m3"
                }
            });

        var tree = await getTree.ExecuteAsync(data.Director.Id, project.Id);
        tree.Nodes.Select(node => node.Code).Should().ContainInOrder("1", "CIM.1");

        var moved = await moveNode.ExecuteAsync(
            data.Director.Id,
            project.Id,
            new MoveWbsNodeCommand
            {
                NodeType = WbsNodeTypeCodes.Item,
                NodeId = item.Id,
                TargetParentChapterId = root.Id,
                TargetIndex = 1,
                RowVersion = item.RowVersion
            });
        moved.ParentChapterId.Should().Be(root.Id);

        var reordered = await reorder.ExecuteAsync(
            data.Director.Id,
            project.Id,
            new ReorderWbsNodesCommand
            {
                ParentChapterId = root.Id,
                Nodes =
                [
                    new WbsReorderNodeDto { NodeType = WbsNodeTypeCodes.Item, NodeId = item.Id },
                    new WbsReorderNodeDto { NodeType = WbsNodeTypeCodes.Chapter, NodeId = child.Id }
                ]
            });

        reordered.Nodes.Select(node => node.NodeId).Should().ContainInOrder(item.Id, child.Id);

        tree = await getTree.ExecuteAsync(data.Director.Id, project.Id);
        tree.Nodes.Should().ContainSingle();
        tree.Nodes[0].Children.Select(node => node.Code).Should().ContainInOrder("CIM.1", "1.1");

        var found = await search.ExecuteAsync(
            data.Director.Id,
            project.Id,
            new SearchWbsQuery { Search = "cim" });
        found.TotalItems.Should().Be(1);
        found.Items[0].Code.Should().Be("CIM.1");

        var projectActivity = await activity.ExecuteAsync(data.Director.Id, project.Id);
        projectActivity.LastActivity.Should().NotBeNull();
        projectActivity.Participants.Should().ContainSingle()
            .Which.UserId.Should().Be(data.Director.Id);

        await deleteItem.ExecuteAsync(
            data.Director.Id,
            project.Id,
            item.Id,
            new DeleteWbsItemCommand { RowVersion = item.RowVersion });

        await deleteChapter.ExecuteAsync(
            data.Director.Id,
            project.Id,
            child.Id,
            new DeleteWbsChapterCommand { RowVersion = child.RowVersion });

        tree = await getTree.ExecuteAsync(data.Director.Id, project.Id);

        await deleteChapter.ExecuteAsync(
            data.Director.Id,
            project.Id,
            root.Id,
            new DeleteWbsChapterCommand { RowVersion = tree.Nodes[0].RowVersion });

        var emptyTree = await getTree.ExecuteAsync(data.Director.Id, project.Id);
        emptyTree.Nodes.Should().BeEmpty();

        var activityAfterDelete = await activity.ExecuteAsync(data.Director.Id, project.Id);
        activityAfterDelete.Participants.Should().ContainSingle();
    }

    [Fact]
    public async Task La_baja_de_un_hijo_compacta_las_posiciones_de_sus_hermanos()
    {
        // SC-EDT-35: sin huecos y sin renumerar códigos.
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var project = TestDataFactory.CreateProject(data, "Proyecto compactación");
        context.Projects.Add(project);
        context.ProjectMembers.Add(TestDataFactory.CreateProjectMember(data, project));
        await context.SaveChangesAsync();

        var accessService = new WbsAccessService(
            new ActorAccessReader(context),
            new WbsProjectAccessReader(context));
        var repository = new WbsRepository(context);
        var apuGateway = new WbsApuGateway(context);
        var createChapter = new CreateWbsChapterCommandHandler(
            accessService,
            repository,
            new CreateWbsChapterCommandValidator());
        var deleteChapter = new DeleteWbsChapterCommandHandler(
            accessService,
            repository,
            apuGateway,
            new DeleteWbsChapterCommandValidator());
        var getTree = new GetWbsTreeQueryHandler(
            accessService,
            repository,
            apuGateway,
            new ApuCostService(new ProjectCostReader(context)));

        var root = await createChapter.ExecuteAsync(
            data.Director.Id,
            project.Id,
            new CreateWbsChapterCommand { Code = "1", Name = "Raíz" });
        await createChapter.ExecuteAsync(
            data.Director.Id,
            project.Id,
            new CreateWbsChapterCommand { Code = "1.1", Name = "A", ParentChapterId = root.Id });
        var middle = await createChapter.ExecuteAsync(
            data.Director.Id,
            project.Id,
            new CreateWbsChapterCommand { Code = "1.2", Name = "B", ParentChapterId = root.Id });
        await createChapter.ExecuteAsync(
            data.Director.Id,
            project.Id,
            new CreateWbsChapterCommand { Code = "1.3", Name = "C", ParentChapterId = root.Id });

        await deleteChapter.ExecuteAsync(
            data.Director.Id,
            project.Id,
            middle.Id,
            new DeleteWbsChapterCommand { RowVersion = middle.RowVersion });

        var tree = await getTree.ExecuteAsync(data.Director.Id, project.Id);
        var children = tree.Nodes[0].Children;

        children.Select(node => node.Code).Should().ContainInOrder("1.1", "1.3");
        children.Select(node => node.OrderIndex).Should().ContainInOrder(0, 1);
    }

    [Fact]
    public async Task El_aislamiento_y_los_permisos_se_respetan()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var dataA = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var dataB = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var projectA = TestDataFactory.CreateProject(dataA, "Proyecto A");
        context.Projects.Add(projectA);
        await context.SaveChangesAsync();

        var auditor = await CreateUserAsync(context, dataA, "AUDITOR");
        var administrator = await CreateUserAsync(context, dataA, "ADMINISTRADOR", memberOfCompany: false);
        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectA.Id,
            UserId = auditor.Id,
            RoleInProject = "AUDITOR",
            AssignedByUserId = dataA.Director.Id,
            AssignedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var accessService = new WbsAccessService(
            new ActorAccessReader(context),
            new WbsProjectAccessReader(context));
        var repository = new WbsRepository(context);
        var apuGateway = new WbsApuGateway(context);
        var getTree = new GetWbsTreeQueryHandler(
            accessService,
            repository,
            apuGateway,
            new ApuCostService(new ProjectCostReader(context)));
        var createChapter = new CreateWbsChapterCommandHandler(
            accessService,
            repository,
            new CreateWbsChapterCommandValidator());
        var activity = new GetWbsActivityQueryHandler(accessService, repository);

        // SC-EDT-56: otra compañía no lee.
        var crossCompanyRead = () => getTree.ExecuteAsync(dataB.Director.Id, projectA.Id);
        var readException = await crossCompanyRead.Should().ThrowAsync<ForbiddenAppException>();
        readException.Which.Code.Should().Be("WBS_READ_FORBIDDEN");

        // SC-EDT-47: el Administrador lee cualquier proyecto pero no escribe (SC-EDT-45).
        await getTree.Invoking(handler => handler.ExecuteAsync(administrator.Id, projectA.Id))
            .Should().NotThrowAsync();

        var adminWrite = () => createChapter.ExecuteAsync(
            administrator.Id,
            projectA.Id,
            new CreateWbsChapterCommand { Code = "ADM", Name = "No permitido" });
        var writeException = await adminWrite.Should().ThrowAsync<ForbiddenAppException>();
        writeException.Which.Code.Should().Be("WBS_WRITE_FORBIDDEN");

        // SC-EDT-49 y SC-EDT-76: el Auditor asignado lee el árbol, pero no la actividad.
        await getTree.Invoking(handler => handler.ExecuteAsync(auditor.Id, projectA.Id))
            .Should().NotThrowAsync();

        var activityException = await activity
            .Invoking(handler => handler.ExecuteAsync(auditor.Id, projectA.Id))
            .Should().ThrowAsync<ForbiddenAppException>();
        activityException.Which.Code.Should().Be("WBS_READ_FORBIDDEN");
    }

    private static async Task<UserAccount> CreateUserAsync(
        BudgetCivilDbContext context,
        CompanyTestData data,
        string roleCode,
        bool memberOfCompany = true)
    {
        var role = await context.Roles.SingleAsync(existingRole => existingRole.Code == roleCode);
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            Email = $"{roleCode}.{suffix}@example.com".ToLowerInvariant(),
            Username = $"{roleCode}_{suffix}".ToLowerInvariant(),
            PasswordHash = "hash",
            AccountEnabled = true,
            FirstName = roleCode,
            LastName = "Prueba",
            DocumentType = "CC",
            DocumentNumber = $"DOC-{suffix}",
            BirthDate = new DateOnly(1990, 1, 1),
            CountryId = data.Country.Id,
            DepartmentId = data.Department.Id,
            MunicipalityId = data.Municipality.Id
        };

        context.UserAccounts.Add(user);

        if (memberOfCompany)
        {
            user.Memberships.Add(CompanyMembership.Create(user.Id, data.Company.Id, DateTime.UtcNow));
        }

        await context.SaveChangesAsync();
        return user;
    }
}
