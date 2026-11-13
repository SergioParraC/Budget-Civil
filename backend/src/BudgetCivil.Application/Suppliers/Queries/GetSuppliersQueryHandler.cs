using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Suppliers.Dtos;
using BudgetCivil.Application.Suppliers.Repositories;
using BudgetCivil.Application.Suppliers.Services;

namespace BudgetCivil.Application.Suppliers.Queries;

public sealed class GetSuppliersQuery
{
    public string? BusinessName { get; init; }

    public string? ContactName { get; init; }

    public Guid? CountryId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? MunicipalityId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public sealed class GetSuppliersResponse
{
    public IReadOnlyList<SupplierDto> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}

/// <summary>Listado completo con filtros y paginación (`REQ-PROVEEDORES-08`, `D-96`, `D-101`).</summary>
public sealed class GetSuppliersQueryHandler(
    SupplierAccessService supplierAccessService,
    ISupplierRepository supplierRepository,
    ListingQueryPipeline listingQueryPipeline)
{
    public async Task<GetSuppliersResponse> ExecuteAsync(
        Guid actorUserId,
        Guid? companyId,
        GetSuppliersQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await supplierAccessService.EnsureReadAsync(actorUserId, companyId, cancellationToken);

        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(
            query.Page,
            query.PageSize,
            cancellationToken);

        var filter = new SupplierFilter(
            scope.CompanyId,
            query.BusinessName,
            query.ContactName,
            query.CountryId,
            query.DepartmentId,
            query.MunicipalityId);

        var items = await supplierRepository.ListAsync(
            filter,
            pagination.Page,
            pagination.PageSize,
            cancellationToken);

        var totalItems = await supplierRepository.CountAsync(filter, cancellationToken);

        return new GetSuppliersResponse
        {
            Items = items.Select(SupplierMapper.ToDto).ToList(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = totalItems
        };
    }
}
