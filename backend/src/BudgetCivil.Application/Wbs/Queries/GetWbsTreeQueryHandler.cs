using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;
using BudgetCivil.Domain.Wbs;

namespace BudgetCivil.Application.Wbs.Queries;

/// <summary>
/// Árbol completo del proyecto ordenado por posición, con los ítems raíz junto a los capítulos raíz
/// (`REQ-EDT-01`, `REQ-EDT-02`, `SC-EDT-36`, `D-118`).
/// </summary>
public sealed class GetWbsTreeQueryHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository)
{
    public async Task<GetWbsTreeResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await wbsAccessService.EnsureReadAsync(actorUserId, projectId, cancellationToken);

        var chapters = await wbsRepository.GetChaptersAsync(projectId, cancellationToken);
        var items = await wbsRepository.GetItemsAsync(projectId, cancellationToken);

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
                    .Select(WbsMapper.ToItemNode))
                .OrderBy(node => node.OrderIndex)
                .ThenBy(node => node.Id)
                .ToList();

            return WbsMapper.ToChapterNode(chapter, children);
        }

        var roots = chaptersByParent[null]
            .OrderBy(chapter => chapter.OrderIndex)
            .Select(BuildChapter)
            .Concat(itemsByParent[null]
                .OrderBy(item => item.OrderIndex)
                .Select(WbsMapper.ToItemNode))
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
