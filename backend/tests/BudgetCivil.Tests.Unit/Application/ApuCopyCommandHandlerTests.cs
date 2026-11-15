using BudgetCivil.Application.Apus.Commands.CopyApu;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Apus;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-U-22`: la copia de APU crea el ítem destino y rechaza códigos duplicados (`REQ-APUS-09`,
/// `REQ-APUS-21`, `D-122`).
/// </summary>
public sealed class ApuCopyCommandHandlerTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    [Fact]
    public async Task La_copia_rechaza_un_codigo_de_item_duplicado()
    {
        var sourceProjectId = Guid.NewGuid();
        var targetProjectId = Guid.NewGuid();
        var source = Apu.Create(sourceProjectId, "Excavación", "m3", ActorId, DateTime.UtcNow);

        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(ActorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActorAccessSnapshot(
                ActorId,
                CompanyId,
                Guid.NewGuid(),
                "DIRECTOR",
                AccountEnabled: true,
                AccountLocked: false,
                RoleIsActive: true,
                CompanyIsActive: true,
                GrantedPermissionKeys: []));

        var accessReader = new Mock<IApuProjectAccessReader>();
        accessReader
            .Setup(reader => reader.GetAccessAsync(
                It.IsAny<Guid>(), ActorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid projectId, Guid _, CancellationToken _) =>
                new ApuProjectAccess(projectId, CompanyId, IsMember: projectId == targetProjectId));

        var apuRepository = new Mock<IApuRepository>();
        apuRepository
            .Setup(repo => repo.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        var wbsRepository = new Mock<IWbsRepository>();
        wbsRepository
            .Setup(repo => repo.CodeExistsAsync(
                targetProjectId, "1.1", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new CopyApuCommandHandler(
            new ApuAccessService(actorReader.Object, accessReader.Object),
            apuRepository.Object,
            wbsRepository.Object,
            new Mock<IApuResourceCopyWriter>().Object,
            new ApuDetailService(apuRepository.Object, new Mock<IWbsItemReader>().Object),
            new CopyApuCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            targetProjectId,
            new CopyApuCommand
            {
                SourceApuId = source.Id,
                Target = new CopyApuTargetCommand
                {
                    Code = "1.1",
                    QuantityWork = 5m
                }
            });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be(WbsRules.CodeDuplicated);
        wbsRepository.Verify(
            repo => repo.AddItemAsync(It.IsAny<BudgetCivil.Domain.Wbs.WbsItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
