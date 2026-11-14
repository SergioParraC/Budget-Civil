using BudgetCivil.Domain.Wbs;

namespace BudgetCivil.Application.Wbs.Repositories;

public sealed record WbsSearchItem(
    Guid NodeId,
    WbsNodeType NodeType,
    string Code,
    string? Name,
    decimal? QuantityWork,
    Guid? ParentChapterId,
    int OrderIndex);

public sealed record WbsSearchPage(
    int Page,
    int PageSize,
    int TotalItems,
    IReadOnlyList<WbsSearchItem> Items);

public sealed record WbsActivityEntry(Guid UserId, string UserName, DateTime LastModifiedAtUtc);

public sealed record WbsActivitySnapshot(
    WbsActivityEntry? LastActivity,
    IReadOnlyList<WbsActivityEntry> Participants);

/// <summary>
/// Persistencia del módulo `M-03` (`D-54`): árbol, búsqueda, códigos, posiciones, movimiento, cascada y
/// actividad. La aplicación nunca accede a EF (`T-A-03`).
/// </summary>
public interface IWbsRepository
{
    Task<WbsChapter?> GetChapterAsync(
        Guid projectId,
        Guid chapterId,
        CancellationToken cancellationToken = default);

    Task<WbsItem?> GetItemAsync(
        Guid projectId,
        Guid itemId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetNodeCodeIdAsync(
        Guid projectId,
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid projectId,
        string code,
        Guid? excludeCodeId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountChildrenAsync(
        Guid projectId,
        Guid? parentChapterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WbsNodeRef>> GetChildrenAsync(
        Guid projectId,
        Guid? parentChapterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, Guid?>> GetChapterParentsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WbsChapter>> GetChaptersAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WbsItem>> GetItemsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WbsChapter>> GetSubtreeChaptersAsync(
        Guid projectId,
        Guid chapterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WbsItem>> GetItemsByChapterIdsAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> chapterIds,
        CancellationToken cancellationToken = default);

    Task AddChapterAsync(WbsChapter chapter, CancellationToken cancellationToken = default);

    Task AddItemAsync(WbsItem item, CancellationToken cancellationToken = default);

    void RemoveItem(WbsItem item);

    void RemoveCode(WbsCode code);

    Task DeleteSubtreeAsync(
        IReadOnlyList<WbsChapter> chapters,
        IReadOnlyList<WbsItem> items,
        CancellationToken cancellationToken = default);

    Task MoveNodeAsync(
        WbsNodeType nodeType,
        Guid nodeId,
        Guid? targetParentChapterId,
        int targetIndex,
        CancellationToken cancellationToken = default);

    Task ReorderAsync(
        Guid projectId,
        Guid? parentChapterId,
        IReadOnlyList<WbsNodeRef> orderedNodes,
        CancellationToken cancellationToken = default);

    Task CompactAsync(
        Guid projectId,
        Guid? parentChapterId,
        CancellationToken cancellationToken = default);

    void SetOriginalRowVersion(WbsChapter chapter, byte[] rowVersion);

    void SetOriginalRowVersion(WbsItem item, byte[] rowVersion);

    Task UpsertActivityAsync(
        Guid projectId,
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<WbsActivitySnapshot> GetActivityAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<WbsSearchPage> SearchAsync(
        Guid projectId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
