using BudgetCivil.Application.Resources.Commands.ChangeResourceQuotationStatus;
using BudgetCivil.Domain.Resources;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-06` y `T-U-28`: modos de cotización, resumen por insumo e idempotencia (`D-11`, `D-12`).</summary>
public sealed class ResourceQuotationCommandHandlerTests
{
    private static readonly Guid ProjectId = ResourceQuotationTestSupport.ProjectId;
    private static readonly Guid ActorId = ResourceQuotationTestSupport.ActorId;

    [Fact]
    public async Task La_seleccion_cotiza_solo_los_insumos_completos_y_reporta_el_resumen()
    {
        // SC-COTIZACION-01.
        var complete = ResourceQuotationTestSupport.Candidate(QuotationStatus.PorCotizar);
        var incomplete = ResourceQuotationTestSupport.Candidate(
            QuotationStatus.PorCotizar, isComplete: false, brand: null);

        var repository = ResourceQuotationTestSupport.Repository([complete, incomplete]);
        repository
            .Setup(repo => repo.MarkQuotedAsync(
                ProjectId,
                It.IsAny<IReadOnlyCollection<Guid>>(),
                ActorId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        repository
            .Setup(repo => repo.CountPendingAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(4);

        var handler = ResourceQuotationTestSupport.Handler(repository);

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new ChangeResourceQuotationStatusCommand
            {
                Scope = "SELECTION",
                ResourceIds = [complete.ResourceId, incomplete.ResourceId]
            });

        response.Requested.Should().Be(2);
        response.Updated.Should().Be(1);
        response.AlreadyQuoted.Should().Be(0);
        response.Skipped.Should().ContainSingle()
            .Which.ResourceId.Should().Be(incomplete.ResourceId);
        response.Skipped[0].Code.Should().Be(ResourceQuotationRules.IncompleteCode);
        response.PendingResources.Should().Be(4);
        response.AllQuoted.Should().BeFalse();
        response.QuotationStatus.Should().Be(QuotationStatusCodes.Cotizado);
    }

    [Fact]
    public async Task La_operacion_es_idempotente_y_no_reescribe_los_ya_cotizados()
    {
        // SC-COTIZACION-09.
        var quoted = ResourceQuotationTestSupport.Candidate(QuotationStatus.Cotizado);
        var repository = ResourceQuotationTestSupport.Repository([quoted]);

        var handler = ResourceQuotationTestSupport.Handler(repository);

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new ChangeResourceQuotationStatusCommand
            {
                Scope = "SELECTION",
                ResourceIds = [quoted.ResourceId]
            });

        response.Updated.Should().Be(0);
        response.AlreadyQuoted.Should().Be(1);
        response.AllQuoted.Should().BeTrue();
        repository.Verify(
            repo => repo.MarkQuotedAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task El_filtro_omite_las_matrices_sin_componentes_y_los_incompletos()
    {
        // SC-COTIZACION-02, SC-COTIZACION-06 y SC-COTIZACION-07.
        var complete = ResourceQuotationTestSupport.Candidate(QuotationStatus.PorCotizar);
        var withoutBrand = ResourceQuotationTestSupport.Candidate(
            QuotationStatus.FaltaInformacion, isComplete: false, brand: null);
        var emptyMatrix = ResourceQuotationTestSupport.Candidate(
            QuotationStatus.FaltaInformacion,
            isComplete: false,
            isMatrix: true,
            matrixComponents: 0);

        var repository = ResourceQuotationTestSupport.Repository();
        repository
            .Setup(repo => repo.GetByFilterAsync(
                ProjectId,
                It.IsAny<BudgetCivil.Application.Resources.Repositories.ResourceQuotationFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([complete, withoutBrand, emptyMatrix]);
        repository
            .Setup(repo => repo.MarkQuotedAsync(
                ProjectId,
                It.IsAny<IReadOnlyCollection<Guid>>(),
                ActorId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        repository
            .Setup(repo => repo.CountPendingAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(6);

        var handler = ResourceQuotationTestSupport.Handler(repository);

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new ChangeResourceQuotationStatusCommand
            {
                Scope = "FILTER",
                Filter = new ResourceQuotationFilterCommand
                {
                    Description = "cemento",
                    QuotationStatus = "POR_COTIZAR",
                    MinPrice = 0m,
                    MaxPrice = 100000m
                }
            });

        response.Requested.Should().Be(3);
        response.Updated.Should().Be(1);
        response.Skipped.Should().HaveCount(2);
        response.Skipped.Should().OnlyContain(item =>
            item.Code == ResourceQuotationRules.IncompleteCode);
        repository.Verify(
            repo => repo.GetByFilterAsync(
                ProjectId,
                It.Is<BudgetCivil.Application.Resources.Repositories.ResourceQuotationFilter>(filter =>
                    filter.Description == "cemento" &&
                    filter.QuotationStatus == QuotationStatus.PorCotizar &&
                    filter.MinPrice == 0m &&
                    filter.MaxPrice == 100000m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task La_totalidad_cotiza_todo_el_proyecto_en_una_sola_llamada()
    {
        // SC-COTIZACION-03.
        var repository = ResourceQuotationTestSupport.Repository();
        repository
            .Setup(repo => repo.GetAllAsync(ProjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                ResourceQuotationTestSupport.Candidate(QuotationStatus.PorCotizar),
                ResourceQuotationTestSupport.Candidate(QuotationStatus.PorCotizar)
            ]);
        repository
            .Setup(repo => repo.MarkQuotedAsync(
                ProjectId,
                It.IsAny<IReadOnlyCollection<Guid>>(),
                ActorId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var handler = ResourceQuotationTestSupport.Handler(repository);

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new ChangeResourceQuotationStatusCommand { Scope = "ALL" });

        response.Requested.Should().Be(2);
        response.Updated.Should().Be(2);
        response.AllQuoted.Should().BeTrue();
        repository.Verify(
            repo => repo.GetAllAsync(ProjectId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
