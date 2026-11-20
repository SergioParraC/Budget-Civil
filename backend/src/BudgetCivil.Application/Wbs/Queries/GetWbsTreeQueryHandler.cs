using BudgetCivil.Application.Calculation;
using BudgetCivil.Application.Calculation.Services;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Wbs;

namespace BudgetCivil.Application.Wbs.Queries;

/// <summary>
/// Árbol completo del proyecto ordenado por posición, con los ítems raíz junto a los capítulos raíz
/// (`REQ-EDT-01`, `REQ-EDT-02`, `SC-EDT-36`, `D-118`). Los costos se derivan en cada lectura con el
/// servicio compartido de `Application.Calculation` (`REQ-EDT-23`, `D1`, `D2`).
/// </summary>
public sealed class GetWbsTreeQueryHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    IApuSummaryReader apuSummaryReader,
    ApuCostService apuCostService)
{
    public async Task<GetWbsTreeResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await wbsAccessService.EnsureReadAsync(actorUserId, projectId, cancellationToken);

        var chapters = await wbsRepository.GetChaptersAsync(projectId, cancellationToken);
        var items = await wbsRepository.GetItemsAsync(projectId, cancellationToken);
        var apuSummaries = await apuSummaryReader.GetByProjectAsync(projectId, cancellationToken);
        var costSnapshot = await apuCostService.GetProjectSnapshotAsync(projectId, cancellationToken);

        var itemCosts = costSnapshot is null
            ? new Dictionary<Guid, ProjectItemCost>()
            : costSnapshot.Items.ToDictionary(cost => cost.ItemId);

        var chapterCosts = costSnapshot is null
            ? new Dictionary<Guid, decimal?>()
            : WbsCostAggregation.AggregateChapterCosts(
                chapters
                    .Select(chapter => new ChapterCostNode(chapter.Id, chapter.ParentChapterId))
                    .ToList(),
                items
                    .Where(item => item.WbsChapterId.HasValue)
                    .Select(item => new ChapterCostItem(
                        item.WbsChapterId!.Value,
                        itemCosts.TryGetValue(item.Id, out var cost) ? cost.DirectCost : null))
                    .ToList(),
                costSnapshot.Settings.RoundingDecimals);

        WbsNodeDto BuildItem(WbsItem item)
        {
            var summary = item.ApuId.HasValue && apuSummaries.TryGetValue(item.ApuId.Value, out var found)
                ? found
                : null;

            itemCosts.TryGetValue(item.Id, out var cost);

            return WbsMapper.ToItemNode(item, summary, cost?.UnitCost, cost?.DirectCost);
        }

        var chaptersByParent = chapters.ToLookup(chapter => chapter.ParentChapterId);
        var itemsByParent = items.ToLookup(item => item.WbsChapterId);
        var visited = new HashSet<Guid>();

        WbsNodeDto BuildChapter(WbsChapter chapter)
        {
            if (!visited.Add(chapter.Id))
            {
                return WbsMapper.ToChapterNode(chapter);
            }

            var children = chaptersByParent[chapter.Id]
                .OrderBy(child => child.OrderIndex)
                .Select(BuildChapter)
                .Concat(itemsByParent[chapter.Id]
                    .OrderBy(item => item.OrderIndex)
                    .Select(BuildItem))
                .OrderBy(node => node.OrderIndex)
                .ThenBy(node => node.Id)
                .ToList();

            return WbsMapper.ToChapterNode(
                chapter,
                children,
                chapterCosts.TryGetValue(chapter.Id, out var directCost) ? directCost : null);
        }

        var roots = chaptersByParent[null]
            .OrderBy(chapter => chapter.OrderIndex)
            .Select(BuildChapter)
            .Concat(itemsByParent[null]
                .OrderBy(item => item.OrderIndex)
                .Select(BuildItem))
            .OrderBy(node => node.OrderIndex)
            .ThenBy(node => node.Id)
            .ToList();

        return new GetWbsTreeResponse
        {
            ProjectId = projectId,
            Nodes = roots
        };
    }
}
