using BudgetCivil.Application.Apus.Commands.WbsItemApu;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Resources;
using BudgetCivil.Domain.Wbs;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-U-22`: alta y edición unificadas de ítem EDT + APU (`REQ-EDT-22`, `REQ-APUS-21`).
/// </summary>
public sealed class WbsItemApuCommandHandlersTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    [Fact]
    public async Task El_alta_crea_item_y_apu_vinculados()
    {
        var apuRepository = CreateApuRepository();
        var wbsRepository = CreateWbsRepository();
        var itemReader = new Mock<IWbsItemReader>();
        itemReader
            .Setup(reader => reader.GetItemByApuAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WbsItemInfo(Guid.NewGuid(), ProjectId, "1.1", 10m, null));

        var handler = new CreateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            wbsRepository.Object,
            apuRepository.Object,
            new ApuDetailService(apuRepository.Object, itemReader.Object),
            new CreateWbsItemCommandValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new CreateWbsItemCommand
            {
                Code = "1.1",
                QuantityWork = 10m,
                Apu = new CreateWbsItemApuDraft
                {
                    Description = "Excavación",
                    Unit = "m3",
                    Resources =
                    [
                        new WbsItemApuResourceDraft
                        {
                            ResourceId = Guid.NewGuid(),
                            Consumption = 2m,
                            WastePercentage = 0m
                        }
                    ]
                }
            });

        response.Id.Should().NotBeEmpty();
        response.ApuId.Should().NotBeEmpty();
        response.Code.Should().Be("1.1");
        response.QuantityWork.Should().Be(10m);
        response.Description.Should().Be("Excavación");
        response.Unit.Should().Be("m3");

        apuRepository.Verify(
            repo => repo.AddAsync(It.IsAny<Apu>(), It.IsAny<CancellationToken>()),
            Times.Once);
        wbsRepository.Verify(
            repo => repo.AddItemAsync(It.IsAny<WbsItem>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task El_alta_exige_el_bloque_apu()
    {
        var handler = new CreateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            CreateWbsRepository().Object,
            CreateApuRepository().Object,
            new ApuDetailService(CreateApuRepository().Object, new Mock<IWbsItemReader>().Object),
            new CreateWbsItemCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new CreateWbsItemCommand { Code = "1.1", QuantityWork = 10m });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task La_edicion_sin_bloque_apu_conserva_la_composicion()
    {
        var apu = Apu.Create(ProjectId, "Excavación", "m3", ActorId, DateTime.UtcNow);
        Set(apu, nameof(Apu.RowVersion), new byte[] { 1, 2, 3 });
        var item = CreateItem(apu.Id);
        var apuRepository = CreateApuRepository(apu);
        var wbsRepository = CreateWbsRepository(item);
        var itemReader = new Mock<IWbsItemReader>();
        itemReader
            .Setup(reader => reader.GetItemByApuAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WbsItemInfo(item.Id, ProjectId, "1.1", 10m, apu.Id));

        var handler = new UpdateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            wbsRepository.Object,
            apuRepository.Object,
            new ApuDetailService(apuRepository.Object, itemReader.Object),
            new UpdateWbsItemCommandValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            item.Id,
            new UpdateWbsItemCommand
            {
                QuantityWork = 15m,
                RowVersion = Convert.ToBase64String(item.RowVersion)
            });

        response.QuantityWork.Should().Be(15m);
        response.Description.Should().Be("Excavación");
    }

    [Fact]
    public async Task La_edicion_que_solo_cambia_el_apu_ignora_una_version_de_item_desactualizada()
    {
        var apu = Apu.Create(ProjectId, "Excavación", "m3", ActorId, DateTime.UtcNow);
        Set(apu, nameof(Apu.RowVersion), new byte[] { 4, 5, 6 });
        var item = CreateItem(apu.Id);
        var apuRepository = CreateApuRepository(apu);
        var wbsRepository = CreateWbsRepository(item);
        var itemReader = new Mock<IWbsItemReader>();
        itemReader
            .Setup(reader => reader.GetItemByApuAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WbsItemInfo(item.Id, ProjectId, "1.1", 10m, apu.Id));

        var handler = new UpdateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            wbsRepository.Object,
            apuRepository.Object,
            new ApuDetailService(apuRepository.Object, itemReader.Object),
            new UpdateWbsItemCommandValidator());

        // El cliente conserva una versión del ítem anterior, pero no cambia código ni cantidad de obra:
        // el guardado del APU no debe fallar por un cambio ajeno en el nodo.
        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            item.Id,
            new UpdateWbsItemCommand
            {
                QuantityWork = 10m,
                RowVersion = Convert.ToBase64String([9, 9, 9]),
                Apu = new UpdateWbsItemApuDraft
                {
                    Description = "Excavación actualizada",
                    Unit = "m3",
                    ApuRowVersion = Convert.ToBase64String(apu.RowVersion)
                }
            });

        response.Description.Should().Be("Excavación actualizada");
        response.QuantityWork.Should().Be(10m);
        wbsRepository.Verify(
            repo => repo.SetOriginalRowVersion(It.IsAny<WbsItem>(), It.IsAny<byte[]>()),
            Times.Never);
    }

    [Fact]
    public async Task La_edicion_que_agrega_un_insumo_registra_la_linea_como_nueva()
    {
        var apu = Apu.Create(ProjectId, "Excavación", "m3", ActorId, DateTime.UtcNow);
        Set(apu, nameof(Apu.RowVersion), new byte[] { 4, 5, 6 });
        var item = CreateItem(apu.Id);
        var apuRepository = CreateApuRepository(apu);
        var wbsRepository = CreateWbsRepository(item);
        var itemReader = new Mock<IWbsItemReader>();
        itemReader
            .Setup(reader => reader.GetItemByApuAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WbsItemInfo(item.Id, ProjectId, "1.1", 10m, apu.Id));

        var handler = new UpdateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            wbsRepository.Object,
            apuRepository.Object,
            new ApuDetailService(apuRepository.Object, itemReader.Object),
            new UpdateWbsItemCommandValidator());

        await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            item.Id,
            new UpdateWbsItemCommand
            {
                QuantityWork = 10m,
                RowVersion = Convert.ToBase64String(item.RowVersion),
                Apu = new UpdateWbsItemApuDraft
                {
                    Description = "Excavación",
                    Unit = "m3",
                    ApuRowVersion = Convert.ToBase64String(apu.RowVersion),
                    Resources =
                    [
                        new WbsItemApuResourceUpdateDraft
                        {
                            ResourceId = Guid.NewGuid(),
                            Consumption = 2m,
                            WastePercentage = 0m
                        }
                    ]
                }
            });

        apuRepository.Verify(
            repo => repo.AddLine(It.IsAny<ApuResource>()),
            Times.Once);
        apuRepository.Verify(
            repo => repo.RemoveLine(It.IsAny<ApuResource>()),
            Times.Never);
    }

    [Fact]
    public async Task La_edicion_que_cambia_el_insumo_de_una_linea_reemplaza_la_linea_y_registra_la_nueva()
    {
        var apu = Apu.Create(ProjectId, "Excavación", "m3", ActorId, DateTime.UtcNow);
        Set(apu, nameof(Apu.RowVersion), new byte[] { 4, 5, 6 });
        var line = apu.AddLine(Guid.NewGuid(), 1m, 0m, ActorId, DateTime.UtcNow);
        var item = CreateItem(apu.Id);
        var apuRepository = CreateApuRepository(apu);
        var wbsRepository = CreateWbsRepository(item);
        var itemReader = new Mock<IWbsItemReader>();
        itemReader
            .Setup(reader => reader.GetItemByApuAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WbsItemInfo(item.Id, ProjectId, "1.1", 10m, apu.Id));

        var handler = new UpdateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            wbsRepository.Object,
            apuRepository.Object,
            new ApuDetailService(apuRepository.Object, itemReader.Object),
            new UpdateWbsItemCommandValidator());

        await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            item.Id,
            new UpdateWbsItemCommand
            {
                QuantityWork = 10m,
                RowVersion = Convert.ToBase64String(item.RowVersion),
                Apu = new UpdateWbsItemApuDraft
                {
                    Description = "Excavación",
                    Unit = "m3",
                    ApuRowVersion = Convert.ToBase64String(apu.RowVersion),
                    Resources =
                    [
                        new WbsItemApuResourceUpdateDraft
                        {
                            LineId = line.Id,
                            ResourceId = Guid.NewGuid(),
                            Consumption = 2m,
                            WastePercentage = 0m
                        }
                    ]
                }
            });

        apuRepository.Verify(repo => repo.RemoveLine(line), Times.Once);
        apuRepository.Verify(
            repo => repo.AddLine(It.Is<ApuResource>(added => added.ResourceId != line.ResourceId)),
            Times.Once);
    }

    [Fact]
    public async Task La_edicion_del_item_con_version_desactualizada_responde_conflicto()
    {
        var apu = Apu.Create(ProjectId, "Excavación", "m3", ActorId, DateTime.UtcNow);
        Set(apu, nameof(Apu.RowVersion), new byte[] { 1, 2, 3 });
        var item = CreateItem(apu.Id);
        var apuRepository = CreateApuRepository(apu);
        var wbsRepository = CreateWbsRepository(item);

        var handler = new UpdateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            wbsRepository.Object,
            apuRepository.Object,
            new ApuDetailService(apuRepository.Object, new Mock<IWbsItemReader>().Object),
            new UpdateWbsItemCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            ProjectId,
            item.Id,
            new UpdateWbsItemCommand
            {
                QuantityWork = 15m,
                RowVersion = Convert.ToBase64String([9, 9, 9])
            });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be(WbsRules.ConcurrencyConflict);
    }

    [Fact]
    public async Task La_edicion_del_apu_con_version_desactualizada_responde_conflicto()
    {
        var apu = Apu.Create(ProjectId, "Excavación", "m3", ActorId, DateTime.UtcNow);
        Set(apu, nameof(Apu.RowVersion), new byte[] { 1, 2, 3 });
        var item = CreateItem(apu.Id);
        var apuRepository = CreateApuRepository(apu);
        var wbsRepository = CreateWbsRepository(item);

        var handler = new UpdateWbsItemCommandHandler(
            WbsTestSupport.AccessService(),
            wbsRepository.Object,
            apuRepository.Object,
            new ApuDetailService(apuRepository.Object, new Mock<IWbsItemReader>().Object),
            new UpdateWbsItemCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            ProjectId,
            item.Id,
            new UpdateWbsItemCommand
            {
                QuantityWork = 15m,
                RowVersion = Convert.ToBase64String(item.RowVersion),
                Apu = new UpdateWbsItemApuDraft
                {
                    Description = "Excavación",
                    Unit = "m3",
                    ApuRowVersion = Convert.ToBase64String([9, 9, 9])
                }
            });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be(ApuRules.ApuConcurrencyConflict);
    }

    private static Mock<IApuRepository> CreateApuRepository(Apu? apu = null)
    {
        var repository = new Mock<IApuRepository>();
        repository
            .Setup(repo => repo.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(
                (operation, token) => operation(token));
        repository
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(repo => repo.GetCostPayloadAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApuCostPayload?)null);
        repository
            .Setup(repo => repo.GetProjectSettingsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectCostSettings(ProjectId, CompanyId, 10m, 5m, 8m, 2, 2));
        repository
            .Setup(repo => repo.GetProjectResourceAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid projectId, Guid resourceId, CancellationToken _) => new Resource
            {
                Id = resourceId,
                ProjectId = projectId,
                Description = "Insumo",
                Unit = "un"
            });

        if (apu is not null)
        {
            repository
                .Setup(repo => repo.GetAsync(ProjectId, apu.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(apu);
        }

        return repository;
    }

    private static Mock<IWbsRepository> CreateWbsRepository(WbsItem? item = null)
    {
        var repository = new Mock<IWbsRepository>();
        repository
            .Setup(repo => repo.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(
                (operation, token) => operation(token));
        repository
            .Setup(repo => repo.CodeExistsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(repo => repo.CountChildrenAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        repository
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(repo => repo.UpsertActivityAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        if (item is not null)
        {
            repository
                .Setup(repo => repo.GetItemAsync(ProjectId, item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(item);
        }

        return repository;
    }

    private static WbsItem CreateItem(Guid apuId)
    {
        var item = WbsItem.Create(
            ProjectId,
            null,
            WbsCode.Create(ProjectId, "1.1", WbsNodeType.Item),
            10m,
            0,
            ActorId,
            DateTime.UtcNow);

        Set(item, nameof(WbsItem.Id), Guid.NewGuid());
        Set(item, nameof(WbsItem.RowVersion), new byte[] { 1, 2, 3 });
        Set(item, nameof(WbsItem.ApuId), apuId);
        return item;
    }

    private static void Set(object target, string property, object value)
    {
        target.GetType().GetProperty(property)!.SetValue(target, value);
    }
}
