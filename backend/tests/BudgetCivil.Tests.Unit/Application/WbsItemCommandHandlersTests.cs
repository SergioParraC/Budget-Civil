using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Commands.DeleteWbsItem;
using BudgetCivil.Application.Wbs.Repositories;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-U-22`: baja del ítem y del APU vinculado (`SC-EDT-81`, `SC-EDT-85`, `REQ-EDT-21`). El alta y la
/// edición unificadas se cubren en <see cref="WbsItemApuCommandHandlersTests"/>.
/// </summary>
public sealed class WbsItemCommandHandlersTests
{
    private static readonly Guid ItemId = Guid.NewGuid();

    [Fact]
    public async Task La_baja_de_un_item_con_version_desactualizada_es_conflicto()
    {
        var repository = new Mock<IWbsRepository>();
        var item = WbsTestSupport.Item(ItemId, null, "CIM.1", 5m);
        repository
            .Setup(repo => repo.GetItemAsync(
                WbsTestSupport.ProjectId, ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var handler = new DeleteWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            WbsTestSupport.ApuCleaner().Object,
            new DeleteWbsItemCommandValidator());

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            ItemId,
            new DeleteWbsItemCommand { RowVersion = WbsTestSupport.RowVersion([8, 8]) });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be("WBS_CONCURRENCY_CONFLICT");
    }

    [Fact]
    public async Task La_baja_de_un_item_elimina_su_codigo_y_su_apu()
    {
        var repository = new Mock<IWbsRepository>()
            .WithTransaction()
            .WithNoOpActivity();
        var apuCleaner = WbsTestSupport.ApuCleaner();
        var item = WbsTestSupport.Item(ItemId, null, "CIM.1", 5m);
        repository
            .Setup(repo => repo.GetItemAsync(
                WbsTestSupport.ProjectId, ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        repository
            .Setup(repo => repo.CompactAsync(
                WbsTestSupport.ProjectId, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            apuCleaner.Object,
            new DeleteWbsItemCommandValidator());

        await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            ItemId,
            new DeleteWbsItemCommand { RowVersion = WbsTestSupport.RowVersion(item.RowVersion) });

        repository.Verify(repo => repo.RemoveItem(item), Times.Once);
        repository.Verify(repo => repo.RemoveCode(item.Code), Times.Once);
        apuCleaner.Verify(
            cleaner => cleaner.DeleteApusForItemsAsync(
                WbsTestSupport.ProjectId,
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(ItemId)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
