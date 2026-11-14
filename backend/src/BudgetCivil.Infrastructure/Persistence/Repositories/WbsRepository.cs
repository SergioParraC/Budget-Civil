using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Domain.Wbs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persistencia del módulo `M-03`: árbol, búsqueda, códigos, posiciones, movimiento, cascada y actividad
/// (`D-54`, `D-118`…`D-120`).
/// </summary>
public sealed class WbsRepository(BudgetCivilDbContext dbContext) : IWbsRepository
{
    private BudgetCivilDbContext DbContext { get; } = dbContext;

    private IQueryable<WbsChapter> Chapters => DbContext.WbsChapters.Include(chapter => chapter.Code);

    private IQueryable<WbsItem> Items => DbContext.WbsItems.Include(item => item.Code);

    public Task<WbsChapter?> GetChapterAsync(
        Guid projectId,
        Guid chapterId,
        CancellationToken cancellationToken = default)
    {
        return Chapters.SingleOrDefaultAsync(
            chapter => chapter.ProjectId == projectId && chapter.Id == chapterId,
            cancellationToken);
    }

    public Task<WbsItem?> GetItemAsync(
        Guid projectId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        return Items.SingleOrDefaultAsync(
            item => item.ProjectId == projectId && item.Id == itemId,
            cancellationToken);
    }

    public async Task<Guid?> GetNodeCodeIdAsync(
        Guid projectId,
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var chapterCodeId = await DbContext.WbsChapters
            .AsNoTracking()
            .Where(chapter => chapter.ProjectId == projectId && chapter.Id == nodeId)
            .Select(chapter => (Guid?)chapter.CodeId)
            .SingleOrDefaultAsync(cancellationToken);

        if (chapterCodeId.HasValue)
        {
            return chapterCodeId;
        }

        return await DbContext.WbsItems
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId && item.Id == nodeId)
            .Select(item => (Guid?)item.CodeId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid projectId,
        string code,
        Guid? excludeCodeId = null,
        CancellationToken cancellationToken = default)
    {
        // La columna usa la intercalación CI, de modo que `cim` y `CIM` colisionan (`BR-015`).
        return DbContext.WbsCodes.AnyAsync(
            existingCode =>
                existingCode.ProjectId == projectId &&
                existingCode.Code == code &&
                (!excludeCodeId.HasValue || existingCode.Id != excludeCodeId.Value),
            cancellationToken);
    }

    public async Task<int> CountChildrenAsync(
        Guid projectId,
        Guid? parentChapterId,
        CancellationToken cancellationToken = default)
    {
        var chapterCount = await DbContext.WbsChapters.CountAsync(
            chapter => chapter.ProjectId == projectId && chapter.ParentChapterId == parentChapterId,
            cancellationToken);

        var itemCount = await DbContext.WbsItems.CountAsync(
            item => item.ProjectId == projectId && item.WbsChapterId == parentChapterId,
            cancellationToken);

        return chapterCount + itemCount;
    }

    public async Task<IReadOnlyList<WbsNodeRef>> GetChildrenAsync(
        Guid projectId,
        Guid? parentChapterId,
        CancellationToken cancellationToken = default)
    {
        var chapters = await DbContext.WbsChapters
            .AsNoTracking()
            .Where(chapter => chapter.ProjectId == projectId && chapter.ParentChapterId == parentChapterId)
            .Select(chapter => new { chapter.Id, chapter.OrderIndex })
            .ToListAsync(cancellationToken);

        var items = await DbContext.WbsItems
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId && item.WbsChapterId == parentChapterId)
            .Select(item => new { item.Id, item.OrderIndex })
            .ToListAsync(cancellationToken);

        return chapters
            .Select(chapter => new WbsNodeRef(WbsNodeType.Chapter, chapter.Id, chapter.OrderIndex))
            .Concat(items.Select(item => new WbsNodeRef(WbsNodeType.Item, item.Id, item.OrderIndex)))
            .OrderBy(node => node.OrderIndex)
            .ThenBy(node => node.NodeId)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, Guid?>> GetChapterParentsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var chapters = await DbContext.WbsChapters
            .AsNoTracking()
            .Where(chapter => chapter.ProjectId == projectId)
            .Select(chapter => new { chapter.Id, chapter.ParentChapterId })
            .ToListAsync(cancellationToken);

        return chapters.ToDictionary(chapter => chapter.Id, chapter => chapter.ParentChapterId);
    }

    public async Task<IReadOnlyList<WbsChapter>> GetChaptersAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await Chapters
            .AsNoTracking()
            .Where(chapter => chapter.ProjectId == projectId)
            .OrderBy(chapter => chapter.OrderIndex)
            .ThenBy(chapter => chapter.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WbsItem>> GetItemsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await Items
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .OrderBy(item => item.OrderIndex)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WbsChapter>> GetSubtreeChaptersAsync(
        Guid projectId,
        Guid chapterId,
        CancellationToken cancellationToken = default)
    {
        var root = await Chapters.SingleOrDefaultAsync(
            chapter => chapter.ProjectId == projectId && chapter.Id == chapterId,
            cancellationToken);

        if (root is null)
        {
            return [];
        }

        var subtree = new List<WbsChapter> { root };
        var frontier = new HashSet<Guid> { root.Id };

        while (frontier.Count > 0)
        {
            var frontierIds = frontier.ToList();
            var children = await Chapters
                .Where(chapter =>
                    chapter.ProjectId == projectId &&
                    chapter.ParentChapterId.HasValue &&
                    frontierIds.Contains(chapter.ParentChapterId.Value))
                .ToListAsync(cancellationToken);

            if (children.Count == 0)
            {
                break;
            }

            subtree.AddRange(children);
            frontier = children.Select(child => child.Id).ToHashSet();
        }

        return subtree;
    }

    public async Task<IReadOnlyList<WbsItem>> GetItemsByChapterIdsAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> chapterIds,
        CancellationToken cancellationToken = default)
    {
        if (chapterIds.Count == 0)
        {
            return [];
        }

        var chapterIdsList = chapterIds.ToList();

        return await Items
            .Where(item =>
                item.ProjectId == projectId &&
                item.WbsChapterId.HasValue &&
                chapterIdsList.Contains(item.WbsChapterId.Value))
            .OrderBy(item => item.OrderIndex)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }


    public Task AddChapterAsync(WbsChapter chapter, CancellationToken cancellationToken = default)
    {
        DbContext.WbsChapters.Add(chapter);
        return Task.CompletedTask;
    }

    public Task AddItemAsync(WbsItem item, CancellationToken cancellationToken = default)
    {
        DbContext.WbsItems.Add(item);
        return Task.CompletedTask;
    }

    public void RemoveItem(WbsItem item)
    {
        DbContext.WbsItems.Remove(item);
    }

    public void RemoveCode(WbsCode code)
    {
        DbContext.WbsCodes.Remove(code);
    }

    public Task DeleteSubtreeAsync(
        IReadOnlyList<WbsChapter> chapters,
        IReadOnlyList<WbsItem> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            DbContext.WbsItems.Remove(item);
        }

        // `D-119`: primero los hijos y al final la raíz para respetar `RESTRICT`.
        foreach (var chapter in WbsTreePolicy.OrderChaptersForDeletion(chapters))
        {
            DbContext.WbsChapters.Remove(chapter);
        }

        var codes = chapters
            .Select(chapter => chapter.Code)
            .Concat(items.Select(item => item.Code))
            .DistinctBy(code => code.Id);

        foreach (var code in codes)
        {
            DbContext.WbsCodes.Remove(code);
        }

        return Task.CompletedTask;
    }

    public async Task MoveNodeAsync(
        WbsNodeType nodeType,
        Guid nodeId,
        Guid? targetParentChapterId,
        int targetIndex,
        CancellationToken cancellationToken = default)
    {
        WbsChapter? chapter = null;
        WbsItem? item = null;
        Guid? sourceParentChapterId;

        if (nodeType == WbsNodeType.Chapter)
        {
            chapter = await GetChapterAsync(DbContext.WbsChapters, nodeId, cancellationToken);
            sourceParentChapterId = chapter?.ParentChapterId;
        }
        else
        {
            item = await GetItemAsync(DbContext.WbsItems, nodeId, cancellationToken);
            sourceParentChapterId = item?.WbsChapterId;
        }

        if (chapter is null && item is null)
        {
            throw new InvalidOperationException("WBS_NOT_FOUND");
        }

        var projectId = chapter?.ProjectId ?? item!.ProjectId;
        var sourceEntries = await LoadChildrenTrackedAsync(projectId, sourceParentChapterId, cancellationToken);

        if (sourceParentChapterId == targetParentChapterId)
        {
            sourceEntries.RemoveAll(entry => entry.NodeId == nodeId && entry.NodeType == nodeType);
            var insertAt = Math.Clamp(targetIndex, 0, sourceEntries.Count);
            sourceEntries.Insert(insertAt, new WbsNodeEntry(nodeType, nodeId, 0, chapter, item));
            ApplyOrder(sourceEntries, targetParentChapterId);
            return;
        }

        var targetEntries = await LoadChildrenTrackedAsync(projectId, targetParentChapterId, cancellationToken);
        sourceEntries.RemoveAll(entry => entry.NodeId == nodeId && entry.NodeType == nodeType);
        targetEntries.RemoveAll(entry => entry.NodeId == nodeId && entry.NodeType == nodeType);

        var targetInsertAt = Math.Clamp(targetIndex, 0, targetEntries.Count);
        targetEntries.Insert(targetInsertAt, new WbsNodeEntry(nodeType, nodeId, 0, chapter, item));

        ApplyOrder(sourceEntries, sourceParentChapterId);
        ApplyOrder(targetEntries, targetParentChapterId);
    }

    public async Task ReorderAsync(
        Guid projectId,
        Guid? parentChapterId,
        IReadOnlyList<WbsNodeRef> orderedNodes,
        CancellationToken cancellationToken = default)
    {
        var entries = await LoadChildrenTrackedAsync(projectId, parentChapterId, cancellationToken);
        var byKey = entries.ToDictionary(entry => (entry.NodeType, entry.NodeId));

        foreach (var orderedNode in orderedNodes)
        {
            if (!byKey.TryGetValue((orderedNode.NodeType, orderedNode.NodeId), out var entry))
            {
                throw new InvalidOperationException("VALIDATION_ERROR");
            }

            SetOrder(entry, parentChapterId, orderedNode.OrderIndex);
        }
    }

    public async Task CompactAsync(
        Guid projectId,
        Guid? parentChapterId,
        CancellationToken cancellationToken = default)
    {
        var entries = await LoadChildrenTrackedAsync(projectId, parentChapterId, cancellationToken);
        ApplyOrder(entries, parentChapterId);
    }

    public void SetOriginalRowVersion(WbsChapter chapter, byte[] rowVersion)
    {
        DbContext.Entry(chapter).Property(entity => entity.RowVersion).OriginalValue = rowVersion;
    }

    public void SetOriginalRowVersion(WbsItem item, byte[] rowVersion)
    {
        DbContext.Entry(item).Property(entity => entity.RowVersion).OriginalValue = rowVersion;
    }


    public async Task UpsertActivityAsync(
        Guid projectId,
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var existing = await DbContext.WbsProjectActivities.FindAsync(
            new object?[] { projectId, userId },
            cancellationToken);

        if (existing is null)
        {
            DbContext.WbsProjectActivities.Add(WbsProjectActivity.Create(projectId, userId, utcNow));
            return;
        }

        existing.Touch(utcNow);
    }

    public async Task<WbsActivitySnapshot> GetActivityAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var participants = await (
                from activity in DbContext.WbsProjectActivities.AsNoTracking()
                join user in DbContext.UserAccounts.AsNoTracking() on activity.UserId equals user.Id
                where activity.ProjectId == projectId
                orderby activity.LastModifiedAtUtc descending, user.Username
                select new WbsActivityEntry(activity.UserId, user.Username, activity.LastModifiedAtUtc))
            .ToListAsync(cancellationToken);

        return new WbsActivitySnapshot(participants.FirstOrDefault(), participants);
    }

    public async Task<WbsSearchPage> SearchAsync(
        Guid projectId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var chapterQuery = Chapters
            .AsNoTracking()
            .Where(chapter => chapter.ProjectId == projectId);

        if (normalizedSearch is not null)
        {
            chapterQuery = chapterQuery.Where(chapter => chapter.Name.Contains(normalizedSearch));
        }

        var itemQuery = Items
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId);

        if (normalizedSearch is not null)
        {
            itemQuery = itemQuery.Where(item => item.Code.Code.Contains(normalizedSearch));
        }

        var chapters = await chapterQuery.ToListAsync(cancellationToken);
        var items = await itemQuery.ToListAsync(cancellationToken);

        var results = chapters
            .Select(chapter => new WbsSearchItem(
                chapter.Id,
                WbsNodeType.Chapter,
                chapter.Code.Code,
                chapter.Name,
                null,
                chapter.ParentChapterId,
                chapter.OrderIndex))
            .Concat(items.Select(item => new WbsSearchItem(
                item.Id,
                WbsNodeType.Item,
                item.Code.Code,
                null,
                item.QuantityWork,
                item.WbsChapterId,
                item.OrderIndex)))
            .OrderBy(result => result.Code, StringComparer.OrdinalIgnoreCase)
            .ThenBy(result => result.NodeId)
            .ToList();

        var pageItems = results
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new WbsSearchPage(page, pageSize, results.Count, pageItems);
    }


    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictAppException(
                "El nodo fue modificado por otro usuario; refresca el árbol y reintenta.",
                "WBS_CONCURRENCY_CONFLICT");
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new ConflictAppException(
                "El código ya existe en el proyecto.",
                "WBS_CODE_DUPLICATED");
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static Task<WbsChapter?> GetChapterAsync(
        IQueryable<WbsChapter> query,
        Guid chapterId,
        CancellationToken cancellationToken)
    {
        return query.SingleOrDefaultAsync(chapter => chapter.Id == chapterId, cancellationToken);
    }

    private static Task<WbsItem?> GetItemAsync(
        IQueryable<WbsItem> query,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        return query.SingleOrDefaultAsync(item => item.Id == itemId, cancellationToken);
    }

    private async Task<List<WbsNodeEntry>> LoadChildrenTrackedAsync(
        Guid projectId,
        Guid? parentChapterId,
        CancellationToken cancellationToken)
    {
        var chapters = await DbContext.WbsChapters
            .Where(chapter => chapter.ProjectId == projectId && chapter.ParentChapterId == parentChapterId)
            .ToListAsync(cancellationToken);

        var items = await DbContext.WbsItems
            .Where(item => item.ProjectId == projectId && item.WbsChapterId == parentChapterId)
            .ToListAsync(cancellationToken);

        return chapters
            .Select(chapter => new WbsNodeEntry(
                WbsNodeType.Chapter,
                chapter.Id,
                chapter.OrderIndex,
                chapter,
                null))
            .Concat(items.Select(item => new WbsNodeEntry(
                WbsNodeType.Item,
                item.Id,
                item.OrderIndex,
                null,
                item)))
            .OrderBy(entry => entry.OrderIndex)
            .ThenBy(entry => entry.NodeId)
            .ToList();
    }

    private static void ApplyOrder(
        IReadOnlyList<WbsNodeEntry> entries,
        Guid? parentChapterId)
    {
        for (var index = 0; index < entries.Count; index++)
        {
            SetOrder(entries[index], parentChapterId, index);
        }
    }

    private static void SetOrder(
        WbsNodeEntry entry,
        Guid? parentChapterId,
        int orderIndex)
    {
        if (entry.Chapter is not null)
        {
            entry.Chapter.MoveTo(parentChapterId, orderIndex);
            return;
        }

        entry.Item!.MoveTo(parentChapterId, orderIndex);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException && sqlException.Number is 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }

    private sealed record WbsNodeEntry(
        WbsNodeType NodeType,
        Guid NodeId,
        int OrderIndex,
        WbsChapter? Chapter,
        WbsItem? Item);
}
