using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Repositories;
using BudgetCivil.Application.Wbs.Services;

namespace BudgetCivil.Application.Wbs.Queries;

public sealed class SearchWbsQuery
{
    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

/// <summary>
/// Búsqueda parcial por código de ítem o nombre de capítulo con paginación `page`/`pageSize`
/// (`REQ-EDT-09`, `SC-EDT-30`…`32`, `D-101`).
/// </summary>
public sealed class SearchWbsQueryHandler(
    WbsAccessService wbsAccessService,
    IWbsRepository wbsRepository,
    ListingQueryPipeline listingQueryPipeline)
{
    public async Task<SearchWbsResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        SearchWbsQuery query,
        CancellationToken cancellationToken = default)
    {
        await wbsAccessService.EnsureReadAsync(actorUserId, projectId, cancellationToken);

        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(
            query.Page,
            query.PageSize,
            cancellationToken);

        var page = await wbsRepository.SearchAsync(
            projectId,
            query.Search,
            pagination.Page,
            pagination.PageSize,
            cancellationToken);

        return new SearchWbsResponse
        {
            Items = page.Items.Select(WbsMapper.ToSearchItem).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalItems = page.TotalItems
        };
    }
}
