using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Domain.Wbs;
using BudgetCivil.Infrastructure.Persistence;
using BudgetCivil.Infrastructure.Persistence.Repositories;
using BudgetCivil.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration;

/// <summary>
/// `T-I-39`: persistencia del EDT — `rowversion`, `CHECK`, unicidad sin distinguir mayúsculas, ítem raíz,
/// índice único filtrado de `apu_id` y una fila por proyecto/usuario en `wbs_project_activity`
/// (`D-113`, `D-115`, `D-118`…`D-120`).
/// </summary>
public sealed class WbsPersistenceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Un_capitulo_y_un_item_raiz_guardan_rowversion_y_codigo()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var project = TestDataFactory.CreateProject(data);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var chapterCode = WbsCode.Create(project.Id, "CIM", WbsNodeType.Chapter);
        var chapter = WbsChapter.Create(
            project.Id,
            null,
            chapterCode,
            "Cimentación",
            0,
            data.Director.Id,
            DateTime.UtcNow);

        var itemCode = WbsCode.Create(project.Id, "CIM.1", WbsNodeType.Item);
        var item = WbsItem.Create(
            project.Id,
            null,
            itemCode,
            12.5m,
            1,
            data.Director.Id,
            DateTime.UtcNow);

        context.WbsChapters.Add(chapter);
        context.WbsItems.Add(item);
        await context.SaveChangesAsync();

        chapter.RowVersion.Should().NotBeEmpty();
        item.RowVersion.Should().NotBeEmpty();
        item.WbsChapterId.Should().BeNull();
        item.ApuId.Should().BeNull();

        var storedCode = await context.WbsCodes.SingleAsync(code => code.Id == itemCode.Id);
        storedCode.Code.Should().Be("CIM.1");
    }

    [Fact]
    public async Task La_unicidad_del_codigo_no_distingue_mayusculas()
    {
        // SC-EDT-24 y BR-015: `cim` colisiona con `CIM`.
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var project = await CreateProjectAsync(context);
        var actorId = await context.UserAccounts.Select(user => user.Id).FirstAsync();

        context.WbsChapters.Add(CreateChapter(project.Id, actorId, "cim", "Capítulo"));
        await context.SaveChangesAsync();

        context.WbsChapters.Add(CreateChapter(project.Id, actorId, "CIM", "Otro capítulo"));
        var action = () => context.SaveChangesAsync();

        var exception = await action.Should().ThrowAsync<DbUpdateException>();
        exception.Which.InnerException.Should().BeOfType<SqlException>()
            .Which.Number.Should().BeOneOf(2601, 2627);
    }

    [Fact]
    public async Task La_base_rechaza_una_cantidad_de_obra_no_positiva()
    {
        // CK_wbs_items_quantity_work y SC-EDT-26.
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var project = await CreateProjectAsync(context);
        var actorId = await context.UserAccounts.Select(user => user.Id).FirstAsync();
        var item = CreateItem(project.Id, actorId, null, "CIM.1", 5m, 0);
        context.WbsItems.Add(item);
        await context.SaveChangesAsync();

        var action = () => context.Database.ExecuteSqlRawAsync(
            "UPDATE wbs_items SET quantity_work = 0 WHERE id = {0}",
            item.Id);

        var exception = await action.Should().ThrowAsync<SqlException>();
        exception.Which.Message.Should().Contain("CK_wbs_items_quantity_work");
    }

    [Fact]
    public async Task El_indice_filtrado_impide_dos_items_con_el_mismo_apu()
    {
        // BR-016 y D-113: `UQ_wbs_items_apu_id` con `WHERE apu_id IS NOT NULL`.
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var project = await CreateProjectAsync(context);
        var actorId = await context.UserAccounts.Select(user => user.Id).FirstAsync();
        var first = CreateItem(project.Id, actorId, null, "CIM.1", 5m, 0);
        var second = CreateItem(project.Id, actorId, null, "CIM.2", 5m, 1);
        context.WbsItems.AddRange(first, second);
        await context.SaveChangesAsync();

        var apuId = Guid.NewGuid();
        var action = async () =>
        {
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE wbs_items SET apu_id = {0} WHERE id IN ({1}, {2})",
                apuId,
                first.Id,
                second.Id);
        };

        var exception = await action.Should().ThrowAsync<SqlException>();
        exception.Which.Message.Should().Contain("UQ_dbo_wbs_items_apu_id");
    }

    [Fact]
    public async Task La_actividad_guarda_una_fila_por_proyecto_y_usuario()
    {
        // SC-EDT-71, SC-EDT-72 y D-120.
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var project = TestDataFactory.CreateProject(data);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var repository = new WbsRepository(context);
        var first = new DateTime(2026, 11, 14, 10, 0, 0, DateTimeKind.Utc);
        var second = first.AddMinutes(5);

        await repository.UpsertActivityAsync(project.Id, data.Director.Id, first);
        await repository.SaveChangesAsync();

        await repository.UpsertActivityAsync(project.Id, data.Director.Id, second);
        await repository.SaveChangesAsync();

        var rows = await context.WbsProjectActivities
            .Where(activity => activity.ProjectId == project.Id)
            .ToListAsync();

        rows.Should().ContainSingle();
        rows[0].LastModifiedAtUtc.Should().Be(second);
    }

    [Fact]
    public async Task El_repositorio_lee_el_arbol_con_los_codigos()
    {
        // T-I-39: lectura del árbol y de las posiciones.
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var project = await CreateProjectAsync(context);
        var actorId = await context.UserAccounts.Select(user => user.Id).FirstAsync();

        var root = CreateChapter(project.Id, actorId, "1", "Capítulo", orderIndex: 0, parentChapterId: null);
        var child = CreateChapter(project.Id, actorId, "1.1", "Subcapítulo", orderIndex: 1, parentChapterId: root.Id);
        var item = CreateItem(project.Id, actorId, root.Id, "1.1.1", 4m, orderIndex: 2);
        context.WbsChapters.AddRange(root, child);
        context.WbsItems.Add(item);
        await context.SaveChangesAsync();

        await using var readContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var repository = new WbsRepository(readContext);
        var chapters = await repository.GetChaptersAsync(project.Id);
        var items = await repository.GetItemsAsync(project.Id);
        var children = await repository.GetChildrenAsync(project.Id, root.Id);

        chapters.Select(chapter => chapter.Code.Code).Should().BeEquivalentTo(["1", "1.1"]);
        items.Should().ContainSingle().Which.Code.Code.Should().Be("1.1.1");
        children.Select(node => node.NodeType).Should().BeEquivalentTo([WbsNodeType.Chapter, WbsNodeType.Item]);
    }

    [Fact]
    public async Task La_version_desactualizada_se_rechaza_sin_modificar_el_nodo()
    {
        // SC-EDT-38 y SC-EDT-39.
        Guid chapterId;

        await using (var setupContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var project = await CreateProjectAsync(setupContext);
            var actorId = await setupContext.UserAccounts.Select(user => user.Id).FirstAsync();
            var chapter = CreateChapter(project.Id, actorId, "1", "Capítulo");
            setupContext.WbsChapters.Add(chapter);
            await setupContext.SaveChangesAsync();
            chapterId = chapter.Id;
        }

        await using var staleContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var staleChapter = await staleContext.WbsChapters
            .Include(chapter => chapter.Code)
            .SingleAsync(chapter => chapter.Id == chapterId);
        var staleRowVersion = staleChapter.RowVersion;

        await using (var winnerContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var winnerChapter = await winnerContext.WbsChapters
                .Include(chapter => chapter.Code)
                .SingleAsync(chapter => chapter.Id == chapterId);
            winnerChapter.Rename("Capítulo ganador");
            await winnerContext.SaveChangesAsync();
        }

        var repository = new WbsRepository(staleContext);
        repository.SetOriginalRowVersion(staleChapter, staleRowVersion);
        staleChapter.Rename("Intento perdido");

        var action = () => repository.SaveChangesAsync();

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be("WBS_CONCURRENCY_CONFLICT");

        await using var verificationContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var stored = await verificationContext.WbsChapters.SingleAsync(chapter => chapter.Id == chapterId);
        stored.Name.Should().Be("Capítulo ganador");
    }

    private static async Task<BudgetCivil.Domain.Projects.Project> CreateProjectAsync(
        BudgetCivilDbContext context)
    {
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var project = TestDataFactory.CreateProject(data);
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

    private static WbsChapter CreateChapter(
        Guid projectId,
        Guid actorUserId,
        string code,
        string name,
        int orderIndex = 0,
        Guid? parentChapterId = null)
    {
        return WbsChapter.Create(
            projectId,
            parentChapterId,
            WbsCode.Create(projectId, code, WbsNodeType.Chapter),
            name,
            orderIndex,
            actorUserId,
            DateTime.UtcNow);
    }

    private static WbsItem CreateItem(
        Guid projectId,
        Guid actorUserId,
        Guid? chapterId,
        string code,
        decimal quantityWork,
        int orderIndex)
    {
        return WbsItem.Create(
            projectId,
            chapterId,
            WbsCode.Create(projectId, code, WbsNodeType.Item),
            quantityWork,
            orderIndex,
            actorUserId,
            DateTime.UtcNow);
    }
}
