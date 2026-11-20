using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Resources.Commands.ChangeResourceQuotationStatus;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using BudgetCivil.Domain.Resources;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

internal static class ResourceQuotationTestSupport
{
    public static readonly Guid ProjectId = Guid.NewGuid();
    public static readonly Guid CompanyId = Guid.NewGuid();
    public static readonly Guid ActorId = Guid.NewGuid();

    public static ChangeResourceQuotationStatusCommandHandler Handler(
        Mock<IResourceQuotationRepository> repository,
        Mock<ISecurityAuditService>? audit = null,
        string roleCode = "DIRECTOR")
    {
        return new ChangeResourceQuotationStatusCommandHandler(
            AccessService(roleCode),
            repository.Object,
            (audit ?? new Mock<ISecurityAuditService>()).Object,
            new ChangeResourceQuotationStatusCommandValidator());
    }

    public static Mock<IResourceQuotationRepository> Repository(
        params ResourceQuotationCandidate[] selection)
    {
        var repository = new Mock<IResourceQuotationRepository>();
        repository
            .Setup(repo => repo.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));
        repository
            .Setup(repo => repo.GetByIdsAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(selection);
        repository
            .Setup(repo => repo.MarkQuotedAsync(
                It.IsAny<Guid>(),
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        repository
            .Setup(repo => repo.CountPendingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        return repository;
    }

    public static ResourceQuotationCandidate Candidate(
        QuotationStatus status,
        bool isComplete = true,
        string? brand = "Marca",
        bool isMatrix = false,
        int matrixComponents = 0)
    {
        return new ResourceQuotationCandidate(
            Guid.NewGuid(),
            "Cemento gris",
            "kg",
            isMatrix ? null : 1000m,
            brand,
            isComplete ? Guid.NewGuid() : null,
            status,
            isComplete,
            isMatrix,
            matrixComponents);
    }

    private static ResourceAccessService AccessService(string roleCode)
    {
        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActorAccessSnapshot(
                ActorId,
                CompanyId,
                Guid.NewGuid(),
                roleCode,
                AccountEnabled: true,
                AccountLocked: false,
                RoleIsActive: true,
                CompanyIsActive: true,
                GrantedPermissionKeys: []));

        var projectReader = new Mock<IResourceProjectAccessReader>();
        projectReader
            .Setup(reader => reader.GetAccessAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceProjectAccess(ProjectId, CompanyId, IsMember: true));

        return new ResourceAccessService(actorReader.Object, projectReader.Object);
    }
}
