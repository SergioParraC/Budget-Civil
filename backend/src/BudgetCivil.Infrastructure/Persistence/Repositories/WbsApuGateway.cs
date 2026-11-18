using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Domain.Apus;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

/// <summary>
/// Puente entre `M-03` (EDT) y `M-05` (APU) sin acoplar los módulos (`D3`, `INC-15`…`INC-17`): lectura
/// del ítem, limpieza del APU, bloqueo de capítulos y resumen del APU del árbol.
/// </summary>
public sealed class WbsApuGateway(BudgetCivilDbContext dbContext) :
    IWbsItemReader,
    IWbsItemApuCleaner,
    IApuSummaryReader
{
    public async Task<WbsItemInfo?> GetItemAsync(
        Guid projectId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.WbsItems
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId && item.Id == itemId)
            .Select(item => new WbsItemInfo(
                item.Id,
                item.ProjectId,
                item.Code.Code,
                item.QuantityWork,
                item.ApuId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<WbsItemInfo?> GetItemByApuAsync(
        Guid apuId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.WbsItems
            .AsNoTracking()
            .Where(item => item.ApuId == apuId)
            .Select(item => new WbsItemInfo(
                item.Id,
                item.ProjectId,
                item.Code.Code,
                item.QuantityWork,
                item.ApuId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, WbsItemInfo>> GetItemsAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var items = await dbContext.WbsItems
            .AsNoTracking()
            .Where(item => item.ProjectId == projectId)
            .Select(item => new WbsItemInfo(
                item.Id,
                item.ProjectId,
                item.Code.Code,
                item.QuantityWork,
                item.ApuId))
            .ToListAsync(cancellationToken);

        return items.ToDictionary(item => item.ItemId);
    }

    public async Task<int> DeleteApusForItemsAsync(
        Guid projectId,
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken = default)
    {
        if (itemIds.Count == 0)
        {
            return 0;
        }

        var apuIds = await dbContext.WbsItems
            .AsNoTracking()
            .Where(item =>
                item.ProjectId == projectId &&
                itemIds.Contains(item.Id) &&
                item.ApuId.HasValue)
            .Select(item => item.ApuId!.Value)
            .ToListAsync(cancellationToken);

        if (apuIds.Count == 0)
        {
            return 0;
        }

        var apus = await dbContext.Apus
            .Where(apu => apuIds.Contains(apu.Id))
            .ToListAsync(cancellationToken);

        dbContext.Apus.RemoveRange(apus);
        return apus.Count;
    }

    public async Task<bool> HasApusInSubtreeAsync(
        Guid projectId,
        Guid chapterId,
        CancellationToken cancellationToken = default)
    {
        var chapters = await dbContext.WbsChapters
            .AsNoTracking()
            .Where(chapter => chapter.ProjectId == projectId)
            .Select(chapter => new { chapter.Id, chapter.ParentChapterId })
            .ToListAsync(cancellationToken);

        var subtreeIds = new HashSet<Guid> { chapterId };
        var changed = true;

        while (changed)
        {
            changed = false;
            foreach (var chapter in chapters)
            {
                if (chapter.ParentChapterId.HasValue &&
                    subtreeIds.Contains(chapter.ParentChapterId.Value) &&
                    subtreeIds.Add(chapter.Id))
                {
                    changed = true;
                }
            }
        }

        return await dbContext.WbsItems
            .AsNoTracking()
            .AnyAsync(
                item =>
                    item.ProjectId == projectId &&
                    item.WbsChapterId.HasValue &&
                    subtreeIds.Contains(item.WbsChapterId.Value) &&
                    item.ApuId.HasValue,
                cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, ApuSummary>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var apus = await dbContext.Apus
            .AsNoTracking()
            .Where(apu => apu.ProjectId == projectId)
            .Select(apu => new
            {
                apu.Id,
                apu.Status,
                apu.Description,
                apu.Unit,
                apu.RowVersion
            })
            .ToListAsync(cancellationToken);

        return apus.ToDictionary(
            apu => apu.Id,
            apu => new ApuSummary(
                apu.Id,
                ApuStatusCodes.ToCode(apu.Status),
                apu.Description,
                apu.Unit,
                Convert.ToBase64String(apu.RowVersion)));
    }
}
