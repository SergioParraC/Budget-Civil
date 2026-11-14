using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Wbs;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

internal static class WbsTestSupport
{
    public static readonly Guid ProjectId = Guid.NewGuid();
    public static readonly Guid CompanyId = Guid.NewGuid();
    public static readonly Guid ActorId = Guid.NewGuid();

    public static WbsAccessService AccessService(
        string roleCode = "DIRECTOR",
        bool isMember = true,
        Guid? actorCompanyId = null)
    {
        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActorAccessSnapshot(
                ActorId,
                actorCompanyId ?? CompanyId,
                Guid.NewGuid(),
                roleCode,
                AccountEnabled: true,
                AccountLocked: false,
                RoleIsActive: true,
                CompanyIsActive: true,
                GrantedPermissionKeys: []));

        var projectReader = new Mock<IWbsProjectAccessReader>();
        projectReader
            .Setup(reader => reader.GetAccessAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WbsProjectAccess(ProjectId, CompanyId, isMember));

        return new WbsAccessService(actorReader.Object, projectReader.Object);
    }

    public static WbsChapter Chapter(
        Guid id,
        Guid? parentChapterId = null,
        string code = "1",
        int orderIndex = 0,
        string name = "Capítulo")
    {
        var chapter = WbsChapter.Create(
            ProjectId,
            parentChapterId,
            WbsCode.Create(ProjectId, code, WbsNodeType.Chapter),
            name,
            orderIndex,
            ActorId,
            DateTime.UtcNow);

        Set(chapter, nameof(WbsChapter.Id), id);
        Set(chapter, nameof(WbsChapter.RowVersion), new byte[] { 1, 2, 3 });
        return chapter;
    }

    public static WbsItem Item(
        Guid id,
        Guid? chapterId,
        string code,
        decimal quantityWork,
        int orderIndex = 0)
    {
        var item = WbsItem.Create(
            ProjectId,
            chapterId,
            WbsCode.Create(ProjectId, code, WbsNodeType.Item),
            quantityWork,
            orderIndex,
            ActorId,
            DateTime.UtcNow);

        Set(item, nameof(WbsItem.Id), id);
        Set(item, nameof(WbsItem.RowVersion), new byte[] { 1, 2, 3 });
        return item;
    }

    public static string RowVersion(byte[] value) => Convert.ToBase64String(value);

    public static Mock<IWbsRepository> WithTransaction(this Mock<IWbsRepository> repository)
    {
        repository
            .Setup(repo => repo.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));

        return repository;
    }

    public static Mock<IWbsRepository> WithNoOpActivity(this Mock<IWbsRepository> repository)
    {
        repository
            .Setup(repo => repo.UpsertActivityAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repository
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return repository;
    }

    private static void Set(object target, string property, object value)
    {
        target.GetType().GetProperty(property)!.SetValue(target, value);
    }
}
