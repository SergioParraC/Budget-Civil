using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Commands.MoveWbsNode;
using BudgetCivil.Application.Wbs.Commands.ReorderWbsNodes;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Domain.Wbs;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-19`: movimiento y reordenamiento (`SC-EDT-33`…`36`, `62`…`70`, `75`).</summary>
public sealed class WbsMoveCommandHandlersTests
{
    private static readonly Guid ChapterId = Guid.NewGuid();
    private static readonly Guid ChildId = Guid.NewGuid();
    private static readonly Guid ItemId = Guid.NewGuid();

    [Fact]
    public async Task Mover_un_capitulo_dentro_de_su_descendiente_es_un_ciclo()
    {
        // SC-EDT-67.
        var repository = new Mock<IWbsRepository>();
        var chapter = WbsTestSupport.Chapter(ChapterId, code: "1");
        var child = WbsTestSupport.Chapter(ChildId, ChapterId, "1.1");
        repository
            .Setup(repo => repo.GetChapterAsync(
                WbsTestSupport.ProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid id, CancellationToken _) =>
                id == ChapterId ? chapter : id == ChildId ? child : null);
        repository
            .Setup(repo => repo.GetChapterParentsAsync(
                WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Guid?>
            {
                [ChapterId] = null,
                [ChildId] = ChapterId
            });

        var handler = new MoveWbsNodeCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new MoveWbsNodeCommandValidator());

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new MoveWbsNodeCommand
            {
                NodeType = WbsNodeTypeCodes.Chapter,
                NodeId = ChapterId,
                TargetParentChapterId = ChildId,
                RowVersion = WbsTestSupport.RowVersion(chapter.RowVersion)
            });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("WBS_MOVE_CYCLE");
    }

    [Fact]
    public async Task Mover_a_un_item_como_destino_es_error_de_validacion()
    {
        // SC-EDT-69.
        var repository = new Mock<IWbsRepository>();
        var chapter = WbsTestSupport.Chapter(ChapterId);
        var targetItem = WbsTestSupport.Item(ItemId, null, "CIM.1", 5m);
        repository
            .Setup(repo => repo.GetChapterAsync(
                WbsTestSupport.ProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid id, CancellationToken _) => id == ChapterId ? chapter : null);
        repository
            .Setup(repo => repo.GetItemAsync(
                WbsTestSupport.ProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid id, CancellationToken _) => id == ItemId ? targetItem : null);
        repository
            .Setup(repo => repo.GetChapterParentsAsync(
                WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Guid?> { [ChapterId] = null });

        var handler = new MoveWbsNodeCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new MoveWbsNodeCommandValidator());

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new MoveWbsNodeCommand
            {
                NodeType = WbsNodeTypeCodes.Chapter,
                NodeId = ChapterId,
                TargetParentChapterId = ItemId,
                RowVersion = WbsTestSupport.RowVersion(chapter.RowVersion)
            });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Mover_a_un_destino_inexistente_es_no_encontrado()
    {
        // SC-EDT-68.
        var repository = new Mock<IWbsRepository>();
        var chapter = WbsTestSupport.Chapter(ChapterId);
        repository
            .Setup(repo => repo.GetChapterAsync(
                WbsTestSupport.ProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid id, CancellationToken _) => id == ChapterId ? chapter : null);
        repository
            .Setup(repo => repo.GetItemAsync(
                WbsTestSupport.ProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WbsItem?)null);
        repository
            .Setup(repo => repo.GetChapterParentsAsync(
                WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Guid?> { [ChapterId] = null });

        var handler = new MoveWbsNodeCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new MoveWbsNodeCommandValidator());

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new MoveWbsNodeCommand
            {
                NodeType = WbsNodeTypeCodes.Chapter,
                NodeId = ChapterId,
                TargetParentChapterId = Guid.NewGuid(),
                RowVersion = WbsTestSupport.RowVersion(chapter.RowVersion)
            });

        var exception = await action.Should().ThrowAsync<NotFoundAppException>();
        exception.Which.Code.Should().Be("WBS_TARGET_NOT_FOUND");
    }

    [Fact]
    public async Task Reordenar_exige_exactamente_los_hijos_vigentes()
    {
        // SC-EDT-34.
        var repository = new Mock<IWbsRepository>();
        var parent = WbsTestSupport.Chapter(ChapterId);
        var first = WbsTestSupport.Chapter(Guid.NewGuid(), ChapterId, "1.1", 0);
        var second = WbsTestSupport.Chapter(Guid.NewGuid(), ChapterId, "1.2", 1);
        repository
            .Setup(repo => repo.GetChapterAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        repository
            .Setup(repo => repo.GetChildrenAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new WbsNodeRef(WbsNodeType.Chapter, first.Id, 0),
                new WbsNodeRef(WbsNodeType.Chapter, second.Id, 1)
            ]);

        var handler = new ReorderWbsNodesCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new ReorderWbsNodesCommandValidator());

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new ReorderWbsNodesCommand
            {
                ParentChapterId = ChapterId,
                Nodes = [new WbsReorderNodeDto { NodeType = WbsNodeTypeCodes.Chapter, NodeId = first.Id }]
            });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Reordenar_guarda_las_posiciones_sin_cambiar_codigos()
    {
        // SC-EDT-17, SC-EDT-34 y SC-EDT-35.
        var repository = new Mock<IWbsRepository>()
            .WithTransaction()
            .WithNoOpActivity();
        var parent = WbsTestSupport.Chapter(ChapterId);
        var first = WbsTestSupport.Chapter(Guid.NewGuid(), ChapterId, "1.1", 0);
        var second = WbsTestSupport.Chapter(Guid.NewGuid(), ChapterId, "1.2", 1);
        repository
            .Setup(repo => repo.GetChapterAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parent);
        repository
            .Setup(repo => repo.GetChildrenAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new WbsNodeRef(WbsNodeType.Chapter, first.Id, 0),
                new WbsNodeRef(WbsNodeType.Chapter, second.Id, 1)
            ]);
        repository
            .Setup(repo => repo.ReorderAsync(
                WbsTestSupport.ProjectId,
                ChapterId,
                It.IsAny<IReadOnlyList<WbsNodeRef>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ReorderWbsNodesCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new ReorderWbsNodesCommandValidator());

        var response = await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new ReorderWbsNodesCommand
            {
                ParentChapterId = ChapterId,
                Nodes =
                [
                    new WbsReorderNodeDto { NodeType = WbsNodeTypeCodes.Chapter, NodeId = second.Id },
                    new WbsReorderNodeDto { NodeType = WbsNodeTypeCodes.Chapter, NodeId = first.Id }
                ]
            });

        response.Nodes.Select(node => node.NodeId).Should().ContainInOrder(second.Id, first.Id);
        response.Nodes[0].OrderIndex.Should().Be(0);
        repository.Verify(
            repo => repo.ReorderAsync(
                WbsTestSupport.ProjectId,
                ChapterId,
                It.Is<IReadOnlyList<WbsNodeRef>>(nodes =>
                    nodes[0].NodeId == second.Id && nodes[0].OrderIndex == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
