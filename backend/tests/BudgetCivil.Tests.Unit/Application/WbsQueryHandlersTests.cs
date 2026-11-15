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
            WbsTestSupport.EmptyApuSummaries().Object);

        var response = await handler.ExecuteAsync(WbsTestSupport.ActorId, WbsTestSupport.ProjectId);

        response.Nodes.Select(node => node.Code).Should().ContainInOrder("1", "CIM.1", "2");
        response.Nodes[0].Children.Should().ContainSingle()
            .Which.Code.Should().Be("1.1");
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
