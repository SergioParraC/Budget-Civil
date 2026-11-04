using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Resources.Dtos;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using BudgetCivil.Domain.Resources;

namespace BudgetCivil.Application.Resources.Queries;

public sealed class GetResourcesQuery
{
    public string? Search { get; init; }

    public Guid? ResourceTypeId { get; init; }

    public string? QuotationStatus { get; init; }

    public string? Brand { get; init; }

    public Guid? MunicipalityId { get; init; }

    public decimal? MinUnitPrice { get; init; }

    public decimal? MaxUnitPrice { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }
}

public sealed class GetResourcesResponse
{
    public IReadOnlyList<ResourceDto> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}

public sealed class GetResourcesQueryHandler(
    ResourceAccessService resourceAccessService,
    IResourceRepository resourceRepository,
    ListingQueryPipeline listingQueryPipeline)
{
    public async Task<GetResourcesResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        GetResourcesQuery query,
        CancellationToken cancellationToken = default)
    {
        await resourceAccessService.EnsureReadAsync(actorUserId, projectId, cancellationToken);

        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(
            query.Page,
            query.PageSize,
            cancellationToken);

        var status = ParseQuotationStatus(query.QuotationStatus);
        var sort = ParseSort(query.SortBy, query.SortDirection);
        await EnsureFiltersAsync(query, cancellationToken);

        var filter = new ResourceFilter(
            projectId,
            query.Search,
            query.ResourceTypeId,
            status,
            query.Brand,
            query.MunicipalityId,
            query.MinUnitPrice,
            query.MaxUnitPrice);

        var items = await resourceRepository.ListAsync(
            filter,
            sort,
            pagination.Page,
            pagination.PageSize,
            cancellationToken);

        var totalItems = await resourceRepository.CountAsync(filter, cancellationToken);

        return new GetResourcesResponse
        {
            Items = items.Select(ResourceMapper.ToDto).ToList(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = totalItems
        };
    }

    private async Task EnsureFiltersAsync(
        GetResourcesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.ResourceTypeId.HasValue &&
            !await resourceRepository.ResourceTypeExistsAsync(query.ResourceTypeId.Value, cancellationToken))
        {
            throw new ValidationAppException("Resource type filter is invalid.", "RESOURCE_TYPE_INVALID");
        }

        if (query.MunicipalityId.HasValue &&
            !await resourceRepository.MunicipalityExistsAsync(query.MunicipalityId.Value, cancellationToken))
        {
            throw new ValidationAppException("Municipality filter is invalid.", "MUNICIPALITY_INVALID");
        }

        if (!string.IsNullOrWhiteSpace(query.Brand) && query.Brand.Trim().Length > Resource.BrandMaxLength)
        {
            throw new ValidationAppException("brand filter is invalid.", "VALIDATION_ERROR");
        }

        if (query.MinUnitPrice is < 0m || query.MaxUnitPrice is < 0m)
        {
            throw new ValidationAppException("Unit price filter cannot be negative.", "VALIDATION_ERROR");
        }

        if (query.MinUnitPrice.HasValue &&
            query.MaxUnitPrice.HasValue &&
            query.MinUnitPrice.Value > query.MaxUnitPrice.Value)
        {
            throw new ValidationAppException("Unit price range is invalid.", "VALIDATION_ERROR");
        }
    }

    private static QuotationStatus? ParseQuotationStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!QuotationStatusCodes.TryParse(value, out var status))
        {
            throw new ValidationAppException("quotationStatus is invalid.", "VALIDATION_ERROR");
        }

        return status;
    }

    private static ResourceSort ParseSort(string? sortBy, string? sortDirection)
    {
        var field = sortBy?.Trim().ToLowerInvariant() switch
        {
            null or "" or "description" => ResourceSortField.Description,
            "unitprice" or "unit_price" => ResourceSortField.UnitPrice,
            "quotationstatus" or "quotation_status" => ResourceSortField.QuotationStatus,
            "lastmodifiedat" or "last_modified_at" => ResourceSortField.LastModifiedAt,
            _ => throw new ValidationAppException("sortBy is invalid.", "VALIDATION_ERROR")
        };

        var direction = sortDirection?.Trim().ToLowerInvariant() switch
        {
            null or "" or "asc" => false,
            "desc" => true,
            _ => throw new ValidationAppException("sortDirection is invalid.", "VALIDATION_ERROR")
        };

        return new ResourceSort(field, direction);
    }
}
