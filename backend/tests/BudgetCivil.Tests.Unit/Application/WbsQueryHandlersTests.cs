using BudgetCivil.Application.Calculation;
using BudgetCivil.Application.Calculation.Repositories;
using BudgetCivil.Application.Calculation.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Pagination;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Wbs.Queries;
using BudgetCivil.Application.Wbs.Repositories;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-18`: consultas del EDT (`SC-EDT-30`…`36`, `58`…`61`, `71`…`76`).</summary>
public sealed class WbsQueryHandlersTests
{
    private static readonly Guid ChapterId = Guid.NewGuid();

    [Fact]
    public async Task La_validacion_de_codigo_invalido_no_consulta_la_base()
    {
        // SC-EDT-60.
        var repository = new Mock<IWbsRepository>();
        var handler = new ValidateWbsCodeQueryHandler(
            WbsTestSupport.AccessService(),
            repository.Object);

        var response = await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new ValidateWbsCodeQuery { Code = "CIM@1" });

        response.IsValid.Should().BeFalse();
        response.IsAvailable.Should().BeFalse();
        response.Reason.Should().Be("WBS_CODE_INVALID");
        repository.Verify(
            repo => repo.CodeExistsAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task La_validacion_detecta_disponibilidad_y_duplicado()
    {
        // SC-EDT-58 y SC-EDT-59.
        var repository = new Mock<IWbsRepository>();
        repository
            .Setup(repo => repo.CodeExistsAsync(
                WbsTestSupport.ProjectId, "CIM.1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(repo => repo.CodeExistsAsync(
                WbsTestSupport.ProjectId, "cim.1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ValidateWbsCodeQueryHandler(
            WbsTestSupport.AccessService(),
            repository.Object);

        var available = await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new ValidateWbsCodeQuery { Code = "CIM.1" });
        var duplicated = await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new ValidateWbsCodeQuery { Code = "cim.1" });

        available.IsValid.Should().BeTrue();
        available.IsAvailable.Should().BeTrue();
        available.Reason.Should().BeNull();
        duplicated.IsValid.Should().BeTrue();
        duplicated.IsAvailable.Should().BeFalse();
        duplicated.Reason.Should().Be("WBS_CODE_DUPLICATED");
    }

    [Fact]
    public async Task La_validacion_excluye_el_nodo_actual()
    {
        // SC-EDT-61.
        var repository = new Mock<IWbsRepository>();
        repository
            .Setup(repo => repo.GetNodeCodeIdAsync(
                WbsTestSupport.ProjectId, ChapterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        repository
            .Setup(repo => repo.CodeExistsAsync(
                WbsTestSupport.ProjectId, "CIM.1", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new ValidateWbsCodeQueryHandler(
            WbsTestSupport.AccessService(),
            repository.Object);

        var response = await handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new ValidateWbsCodeQuery { Code = "CIM.1", ExcludeNodeId = ChapterId });

        response.IsAvailable.Should().BeTrue();
        response.Reason.Should().BeNull();
    }

    [Fact]
    public async Task El_arbol_incluye_los_items_raiz_en_orden()
    {
        // SC-EDT-33 y SC-EDT-36.
        var repository = new Mock<IWbsRepository>();
        var chapter = WbsTestSupport.Chapter(ChapterId, code: "1", orderIndex: 0);
        var otherChapter = WbsTestSupport.Chapter(Guid.NewGuid(), code: "2", orderIndex: 2);
        var childItem = WbsTestSupport.Item(Guid.NewGuid(), ChapterId, "1.1", 3m, orderIndex: 0);
        var rootItem = WbsTestSupport.Item(Guid.NewGuid(), null, "CIM.1", 5m, orderIndex: 1);
        repository
            .Setup(repo => repo.GetChaptersAsync(WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([chapter, otherChapter]);
        repository
            .Setup(repo => repo.GetItemsAsync(WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([childItem, rootItem]);

        var handler = new GetWbsTreeQueryHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            WbsTestSupport.EmptyApuSummaries().Object,
            WbsTestSupport.EmptyCostService());

        var response = await handler.ExecuteAsync(WbsTestSupport.ActorId, WbsTestSupport.ProjectId);

        response.Nodes.Select(node => node.Code).Should().ContainInOrder("1", "CIM.1", "2");
        response.Nodes[0].Children.Should().ContainSingle()
            .Which.Code.Should().Be("1.1");
    }

    [Fact]
    public async Task El_arbol_expone_costo_unitario_y_directo_por_item_y_capitulo()
    {
        // SC-EDT-89, SC-EDT-92 y SC-EDT-93 (T-U-26).
        var chapterId = Guid.NewGuid();
        var childChapterId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var childItemId = Guid.NewGuid();

        var chapter = WbsTestSupport.Chapter(chapterId, code: "1");
        var childChapter = WbsTestSupport.Chapter(childChapterId, parentChapterId: chapterId, code: "1.1");
        var item = WbsTestSupport.Item(itemId, chapterId, "1.2", 10m);
        var childItem = WbsTestSupport.Item(childItemId, childChapterId, "1.1.1", 2m);

        var apuId = Guid.NewGuid();
        item.LinkApu(apuId);
        childItem.LinkApu(apuId);

        var repository = new Mock<IWbsRepository>();
        repository
            .Setup(repo => repo.GetChaptersAsync(WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([chapter, childChapter]);
        repository
            .Setup(repo => repo.GetItemsAsync(WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([item, childItem]);

        var summaries = new Mock<IApuSummaryReader>();
        summaries
            .Setup(reader => reader.GetByProjectAsync(
                WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, ApuSummary>
            {
                [apuId] = new(apuId, "COMPLETO", "Concreto", "M3", "AQ==")
            });

        var costReader = new Mock<IApuCostReader>();
        costReader
            .Setup(reader => reader.GetProjectAsync(
                WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectCostData(
                WbsTestSupport.ProjectId,
                WbsTestSupport.CompanyId,
                new CostSettings(2, 2, 10m, 5m, 8m),
                BudgetLimit: null,
                BaseCurrency: null,
                [
                    new ProjectCostItem(itemId, apuId, 10m, chapterId, 0),
                    new ProjectCostItem(childItemId, apuId, 2m, childChapterId, 0)
                ],
                new Dictionary<Guid, IReadOnlyList<ApuCostLineInput>>
                {
                    [apuId] =
                    [
                        new ApuCostLineInput(
                            Guid.NewGuid(),
                            Guid.NewGuid(),
                            "MATERIAL",
                            "Cemento",
                            "kg",
                            100m,
                            1m,
                            0m)
                    ]
                }));

        var handler = new GetWbsTreeQueryHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            summaries.Object,
            new ApuCostService(costReader.Object));

        var response = await handler.ExecuteAsync(WbsTestSupport.ActorId, WbsTestSupport.ProjectId);

        var root = response.Nodes.Single(node => node.Id == chapterId);
        var child = root.Children.Single(node => node.Id == childChapterId);
        var directItem = root.Children.Single(node => node.Id == itemId);

        directItem.UnitCost.Should().Be(100m);
        directItem.DirectCost.Should().Be(1000m);
        directItem.Apu!.UnitPrice.Should().Be(100m);
        child.Children.Single().DirectCost.Should().Be(200m);
        child.DirectCost.Should().Be(200m);
        root.DirectCost.Should().Be(1200m);
    }

    [Fact]
    public async Task Un_item_sin_apu_devuelve_apu_y_costos_nulos()
    {
        // SC-EDT-90.
        var chapterId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var item = WbsTestSupport.Item(itemId, chapterId, "1.1", 5m);

        var repository = new Mock<IWbsRepository>();
        repository
            .Setup(repo => repo.GetChaptersAsync(WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([WbsTestSupport.Chapter(chapterId, code: "1")]);
        repository
            .Setup(repo => repo.GetItemsAsync(WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var costReader = new Mock<IApuCostReader>();
        costReader
            .Setup(reader => reader.GetProjectAsync(
                WbsTestSupport.ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProjectCostData(
                WbsTestSupport.ProjectId,
                WbsTestSupport.CompanyId,
                new CostSettings(2, 2, 0m, 0m, 0m),
                BudgetLimit: null,
                BaseCurrency: null,
                [new ProjectCostItem(itemId, null, 5m, chapterId, 0)],
                new Dictionary<Guid, IReadOnlyList<ApuCostLineInput>>()));

        var handler = new GetWbsTreeQueryHandler(
            WbsTestSupport.AccessService(),
            repository.Object,
            WbsTestSupport.EmptyApuSummaries().Object,
            new ApuCostService(costReader.Object));

        var response = await handler.ExecuteAsync(WbsTestSupport.ActorId, WbsTestSupport.ProjectId);

        var root = response.Nodes.Single();
        var node = root.Children.Single();

        node.Apu.Should().BeNull();
        node.UnitCost.Should().BeNull();
        node.DirectCost.Should().BeNull();
        root.DirectCost.Should().BeNull();
    }

    [Fact]
    public async Task El_auditor_no_consulta_la_actividad()
    {
        // SC-EDT-76.
        var repository = new Mock<IWbsRepository>();
        var handler = new GetWbsActivityQueryHandler(
            WbsTestSupport.AccessService("AUDITOR"),
            repository.Object);

        var action = () => handler.ExecuteAsync(WbsTestSupport.ActorId, WbsTestSupport.ProjectId);

        var exception = await action.Should().ThrowAsync<ForbiddenAppException>();
        exception.Which.Code.Should().Be("WBS_READ_FORBIDDEN");
    }

    [Fact]
    public async Task La_busqueda_valida_el_tamano_de_pagina()
    {
        // SC-EDT-32.
        var handler = new SearchWbsQueryHandler(
            WbsTestSupport.AccessService(),
            new Mock<IWbsRepository>().Object,
            new ListingQueryPipeline(new PaginationValidator()));

        var action = () => handler.ExecuteAsync(
            WbsTestSupport.ActorId,
            WbsTestSupport.ProjectId,
            new SearchWbsQuery { Search = "cim", Page = 1, PageSize = 25 });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("PAGE_SIZE_INVALID");
    }
}
