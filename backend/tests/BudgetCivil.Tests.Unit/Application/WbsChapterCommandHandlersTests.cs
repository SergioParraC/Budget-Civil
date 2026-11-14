using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Commands.CreateWbsChapter;
using BudgetCivil.Application.Wbs.Commands.DeleteWbsChapter;
using BudgetCivil.Application.Wbs.Commands.UpdateWbsChapter;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Domain.Wbs;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-18`: comandos de capítulo (`SC-EDT-01`, `08`, `09`, `12`, `19`, `53`, `54`).</summary>
public sealed class WbsChapterCommandHandlersTests
{
    private static readonly Guid ChapterId = Guid.NewGuid();

    [Fact]
    public async Task Alta_exige_codigo_manual_valido()
    {
        // SC-EDT-08 y SC-EDT-20.
        var repository = new Mock<IWbsRepository>();
        var handler = new CreateWbsChapterCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new CreateWbsChapterCommandValidator());

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new CreateWbsChapterCommand { Code = "CIM@1", Name = "Cimentación" });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("WBS_CODE_INVALID");
        repository.Verify(
            repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Alta_guarda_codigo_manual_autor_y_actividad()
    {
        // SC-EDT-01, SC-EDT-09 y SC-EDT-71.
        var repository = new Mock<IWbsRepository>().WithNoOpActivity();
        WbsChapter? created = null;
        repository
            .Setup(repo => repo.CodeExistsAsync(
                WbsTestSupport.ProjectId, "EST", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(repo => repo.CountChildrenAsync(
                WbsTestSupport.ProjectId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        repository
            .Setup(repo => repo.AddChapterAsync(It.IsAny<WbsChapter>(), It.IsAny<CancellationToken>()))
            .Callback<WbsChapter, CancellationToken>((chapter, _) => created = chapter)
            .Returns(Task.CompletedTask);

        var handler = new CreateWbsChapterCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new CreateWbsChapterCommandValidator());

        var response = await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new CreateWbsChapterCommand { Code = "EST", Name = "Estructura" });

        created.Should().NotBeNull();
        created!.Code.Code.Should().Be("EST");
        created.ParentChapterId.Should().BeNull();
        created.LastModifiedByUserId.Should().Be(WbsTestSupport.ActorId);
        response.Code.Should().Be("EST");
        response.NodeType.Should().Be(WbsNodeTypeCodes.Chapter);
        repository.Verify(
            repo => repo.UpsertActivityAsync(
                WbsTestSupport.ProjectId,
                WbsTestSupport.ActorId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Alta_rechaza_codigo_duplicado()
    {
        // SC-EDT-12 y SC-EDT-24.
        var repository = new Mock<IWbsRepository>();
        repository
            .Setup(repo => repo.CodeExistsAsync(
                WbsTestSupport.ProjectId, "EST", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CreateWbsChapterCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new CreateWbsChapterCommandValidator());

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new CreateWbsChapterCommand { Code = "EST", Name = "Estructura" });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be("WBS_CODE_DUPLICATED");
    }

    [Fact]
    public async Task Editar_capitulo_cambia_solo_su_codigo_y_nombre()
    {
        // SC-EDT-19 y SC-EDT-37.
        var repository = new Mock<IWbsRepository>().WithNoOpActivity();
        var chapter = WbsTestSupport.Chapter(ChapterId, code: "1");
        repository
            .Setup(repo => repo.GetChapterAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chapter);
        repository
            .Setup(repo => repo.CodeExistsAsync(
                WbsTestSupport.ProjectId,
                "A",
                chapter.CodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new UpdateWbsChapterCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new UpdateWbsChapterCommandValidator());

        var response = await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            ChapterId,
            new UpdateWbsChapterCommand
            {
                Name = "Capítulo",
                Code = "A",
                RowVersion = WbsTestSupport.RowVersion(chapter.RowVersion)
            });

        response.Code.Should().Be("A");
    }

    [Fact]
    public async Task La_baja_con_hijos_sin_confirmacion_es_conflicto()
    {
        // SC-EDT-53.
        var repository = new Mock<IWbsRepository>();
        var chapter = WbsTestSupport.Chapter(ChapterId);
        repository
            .Setup(repo => repo.GetChapterAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chapter);
        repository
            .Setup(repo => repo.GetSubtreeChaptersAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([chapter, WbsTestSupport.Chapter(Guid.NewGuid(), ChapterId, "1.1")]);
        repository
            .Setup(repo => repo.GetItemsByChapterIdsAsync(
                WbsTestSupport.ProjectId,
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var handler = new DeleteWbsChapterCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new DeleteWbsChapterCommandValidator());

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            ChapterId,
            new DeleteWbsChapterCommand
            {
                Cascade = false,
                RowVersion = WbsTestSupport.RowVersion(chapter.RowVersion)
            });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be("WBS_NODE_HAS_CHILDREN");
    }

    [Fact]
    public async Task La_baja_de_un_capitulo_con_solo_items_exige_confirmacion()
    {
        // SC-EDT-53: los ítems también cuentan como descendientes.
        var repository = new Mock<IWbsRepository>();
        var chapter = WbsTestSupport.Chapter(ChapterId);
        repository
            .Setup(repo => repo.GetChapterAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chapter);
        repository
            .Setup(repo => repo.GetSubtreeChaptersAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([chapter]);
        repository
            .Setup(repo => repo.GetItemsByChapterIdsAsync(
                WbsTestSupport.ProjectId,
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([WbsTestSupport.Item(Guid.NewGuid(), ChapterId, "1.1", 2m)]);

        var handler = new DeleteWbsChapterCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new DeleteWbsChapterCommandValidator());

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            ChapterId,
            new DeleteWbsChapterCommand
            {
                Cascade = false,
                RowVersion = WbsTestSupport.RowVersion(chapter.RowVersion)
            });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be("WBS_NODE_HAS_CHILDREN");
    }

    [Fact]
    public async Task La_cascada_confirmada_elimina_el_subarbol_y_compacta()
    {
        // SC-EDT-54 y D-119.
        var repository = new Mock<IWbsRepository>()
            .WithTransaction()
            .WithNoOpActivity();
        var chapter = WbsTestSupport.Chapter(ChapterId);
        var child = WbsTestSupport.Chapter(Guid.NewGuid(), ChapterId, "1.1");
        repository
            .Setup(repo => repo.GetChapterAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chapter);
        repository
            .Setup(repo => repo.GetSubtreeChaptersAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([chapter, child]);
        repository
            .Setup(repo => repo.GetItemsByChapterIdsAsync(
                WbsTestSupport.ProjectId,
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository
            .Setup(repo => repo.DeleteSubtreeAsync(
                It.IsAny<IReadOnlyList<WbsChapter>>(),
                It.IsAny<IReadOnlyList<WbsItem>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(repo => repo.CompactAsync(
                WbsTestSupport.ProjectId, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteWbsChapterCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new DeleteWbsChapterCommandValidator());

        await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            ChapterId,
            new DeleteWbsChapterCommand
            {
                Cascade = true,
                RowVersion = WbsTestSupport.RowVersion(chapter.RowVersion)
            });

        repository.Verify(
            repo => repo.DeleteSubtreeAsync(
                It.Is<IReadOnlyList<WbsChapter>>(chapters => chapters.Count == 2),
                It.IsAny<IReadOnlyList<WbsItem>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        repository.Verify(
            repo => repo.CompactAsync(
                WbsTestSupport.ProjectId, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
