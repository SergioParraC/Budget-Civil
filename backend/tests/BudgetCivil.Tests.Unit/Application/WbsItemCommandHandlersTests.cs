using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Commands.CreateWbsItem;
using BudgetCivil.Application.Wbs.Commands.DeleteWbsItem;
using BudgetCivil.Application.Wbs.Commands.UpdateWbsItem;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Domain.Wbs;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-19`: comandos de ítem (`SC-EDT-06`, `25`…`29`, `39`, `51`, `55`).</summary>
public sealed class WbsItemCommandHandlersTests
{
    private static readonly Guid ItemId = Guid.NewGuid();

    [Fact]
    public async Task Alta_en_la_raiz_deja_capitulo_nulo_y_apu_vacio()
    {
        // SC-EDT-06, SC-EDT-25, SC-EDT-28 y SC-EDT-29.
        var repository = new Mock<IWbsRepository>().WithNoOpActivity();
        WbsItem? created = null;
        repository
            .Setup(repo => repo.CodeExistsAsync(
                WbsTestSupport.ProjectId, "CIM.1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(repo => repo.CountChildrenAsync(
                WbsTestSupport.ProjectId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        repository
            .Setup(repo => repo.AddItemAsync(It.IsAny<WbsItem>(), It.IsAny<CancellationToken>()))
            .Callback<WbsItem, CancellationToken>((item, _) => created = item)
            .Returns(Task.CompletedTask);

        var handler = new CreateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new CreateWbsItemCommandValidator());

        var response = await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new CreateWbsItemCommand { Code = "CIM.1", QuantityWork = 12.5m });

        created.Should().NotBeNull();
        created!.WbsChapterId.Should().BeNull();
        created.ApuId.Should().BeNull();
        created.OrderIndex.Should().Be(2);
        created.QuantityWork.Should().Be(12.5m);
        response.ParentChapterId.Should().BeNull();
        response.QuantityWork.Should().Be(12.5m);
    }

    [Fact]
    public async Task La_cantidad_no_positiva_es_invalida()
    {
        // SC-EDT-26.
        var handler = new CreateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            new Mock<IWbsRepository>().Object,
            new CreateWbsItemCommandValidator());

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new CreateWbsItemCommand { Code = "CIM.1", QuantityWork = 0m });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("WBS_QUANTITY_INVALID");
    }

    [Fact]
    public async Task Editar_con_version_desactualizada_es_conflicto()
    {
        // SC-EDT-39.
        var repository = new Mock<IWbsRepository>();
        var item = WbsTestSupport.Item(ItemId, null, "CIM.1", 5m);
        repository
            .Setup(repo => repo.GetItemAsync(
                WbsTestSupport.ProjectId, ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var handler = new UpdateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new UpdateWbsItemCommandValidator());

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            ItemId,
            new UpdateWbsItemCommand
            {
                Code = "CIM.1",
                QuantityWork = 6m,
                RowVersion = WbsTestSupport.RowVersion([9, 9])
            });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be("WBS_CONCURRENCY_CONFLICT");
    }

    [Fact]
    public async Task Editar_codigo_y_cantidad_con_la_version_vigente()
    {
        // SC-EDT-25 y SC-EDT-37.
        var repository = new Mock<IWbsRepository>().WithNoOpActivity();
        var item = WbsTestSupport.Item(ItemId, null, "CIM.1", 5m);
        repository
            .Setup(repo => repo.GetItemAsync(
                WbsTestSupport.ProjectId, ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        repository
            .Setup(repo => repo.CodeExistsAsync(
                WbsTestSupport.ProjectId,
                "CIM.2",
                item.CodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new UpdateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            new UpdateWbsItemCommandValidator());

        var response = await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            ItemId,
            new UpdateWbsItemCommand
            {
                Code = "CIM.2",
                QuantityWork = 8.25m,
                RowVersion = WbsTestSupport.RowVersion(item.RowVersion)
            });

        response.Code.Should().Be("CIM.2");
        response.QuantityWork.Should().Be(8.25m);
    }

    [Fact]
    public async Task La_baja_de_un_item_con_version_desactualizada_es_conflicto()
    {
        // SC-EDT-39.
        var repository = new Mock<IWbsRepository>();
        var item = WbsTestSupport.Item(ItemId, null, "CIM.1", 5m);
        repository
            .Setup(repo => repo.GetItemAsync(
                WbsTestSupport.ProjectId, ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var handler = new DeleteWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
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
    public async Task La_baja_de_un_item_elimina_su_codigo_y_compacta()
    {
        // SC-EDT-51.
        var repository = new Mock<IWbsRepository>()
            .WithTransaction()
            .WithNoOpActivity();
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
            new DeleteWbsItemCommandValidator());

        await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            ItemId,
            new DeleteWbsItemCommand { RowVersion = WbsTestSupport.RowVersion(item.RowVersion) });

        repository.Verify(repo => repo.RemoveItem(item), Times.Once);
        repository.Verify(repo => repo.RemoveCode(item.Code), Times.Once);
        repository.Verify(
            repo => repo.CompactAsync(WbsTestSupport.ProjectId, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
