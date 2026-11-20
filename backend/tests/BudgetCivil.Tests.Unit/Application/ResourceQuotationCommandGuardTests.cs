using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Commands.ChangeResourceQuotationStatus;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Domain.Resources;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-06` y `T-U-28`: selección ajena, autoría, auditoría y validación del `scope` (`D-11`, `D-12`).</summary>
public sealed class ResourceQuotationCommandGuardTests
{
    private static readonly Guid ProjectId = ResourceQuotationTestSupport.ProjectId;
    private static readonly Guid ActorId = ResourceQuotationTestSupport.ActorId;

    [Fact]
    public async Task Un_insumo_de_otro_proyecto_responde_404_sin_aplicar_el_lote()
    {
        // SC-COTIZACION-05.
        var repository = ResourceQuotationTestSupport.Repository();
        var handler = ResourceQuotationTestSupport.Handler(repository);

        var action = () => handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new ChangeResourceQuotationStatusCommand
            {
                Scope = "SELECTION",
                ResourceIds = [Guid.NewGuid()]
            });

        var exception = await action.Should().ThrowAsync<NotFoundAppException>();
        exception.Which.Code.Should().Be("RESOURCE_NOT_FOUND");
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
    public async Task El_lote_escribe_la_autoria_del_actor_y_el_evento_de_auditoria()
    {
        // SC-COTIZACION-21 y SC-COTIZACION-23.
        var complete = ResourceQuotationTestSupport.Candidate(QuotationStatus.PorCotizar);
        var repository = ResourceQuotationTestSupport.Repository([complete]);
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
            .ReturnsAsync(3);

        var audit = new Mock<ISecurityAuditService>();
        var handler = ResourceQuotationTestSupport.Handler(repository, audit);

        await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new ChangeResourceQuotationStatusCommand
            {
                Scope = "SELECTION",
                ResourceIds = [complete.ResourceId]
            });

        repository.Verify(
            repo => repo.MarkQuotedAsync(
                ProjectId,
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == complete.ResourceId),
                ActorId,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        audit.Verify(
            service => service.WriteAsync(
                It.Is<SecurityAuditWriteRequest>(request =>
                    request.ReasonCode == "RESOURCE_QUOTATION_CHANGED" &&
                    request.PermissionKey == "RESOURCES_WRITE" &&
                    request.Metadata != null &&
                    request.Metadata["updated"] == "1" &&
                    request.Metadata["skipped"] == "0"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData("OTRO", false, false)]
    [InlineData("SELECTION", false, false)]
    [InlineData("FILTER", false, false)]
    [InlineData("ALL", true, false)]
    [InlineData("ALL", false, true)]
    public async Task Las_combinaciones_invalidas_del_alcance_responden_400(
        string? scope,
        bool withIds,
        bool withFilter)
    {
        // SC-COTIZACION-04.
        var repository = ResourceQuotationTestSupport.Repository();
        var handler = ResourceQuotationTestSupport.Handler(repository);

        var action = () => handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new ChangeResourceQuotationStatusCommand
            {
                Scope = scope,
                ResourceIds = withIds ? [Guid.NewGuid()] : null,
                Filter = withFilter ? new ResourceQuotationFilterCommand() : null
            });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Una_seleccion_vacia_es_invalida()
    {
        // SC-COTIZACION-04.
        var handler = ResourceQuotationTestSupport.Handler(
            ResourceQuotationTestSupport.Repository());

        var action = () => handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new ChangeResourceQuotationStatusCommand
            {
                Scope = "SELECTION",
                ResourceIds = []
            });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("VALIDATION_ERROR");
    }
}
