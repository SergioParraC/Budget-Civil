using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

internal static class ResourceAccessTestSupport
{
    public static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static ResourceAccessService Create(
        Guid projectId,
        Guid companyId,
        string roleCode = "DIRECTOR",
        bool isMember = false,
        Guid? actorUserId = null,
        Guid? actorCompanyId = null)
    {
        var actor = new ActorAccessSnapshot(
            actorUserId ?? ActorId,
            actorCompanyId ?? companyId,
            Guid.NewGuid(),
            roleCode,
            AccountEnabled: true,
            AccountLocked: false,
            RoleIsActive: true,
            CompanyIsActive: true,
            GrantedPermissionKeys: []);

        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(actor);

        var projectReader = new Mock<IResourceProjectAccessReader>();
        projectReader
            .Setup(reader => reader.GetAccessAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceProjectAccess(projectId, companyId, isMember));

        return new ResourceAccessService(actorReader.Object, projectReader.Object);
    }
}