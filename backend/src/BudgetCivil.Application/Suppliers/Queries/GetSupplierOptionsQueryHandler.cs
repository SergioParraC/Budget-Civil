using BudgetCivil.Application.Suppliers.Dtos;
using BudgetCivil.Application.Suppliers.Repositories;
using BudgetCivil.Application.Suppliers.Services;

namespace BudgetCivil.Application.Suppliers.Queries;

public sealed class GetSupplierOptionsQuery
{
    public string? BusinessName { get; init; }

    public string? ContactName { get; init; }

    public Guid? CountryId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? MunicipalityId { get; init; }

    public int Limit { get; init; } = 50;
}

public sealed class GetSupplierOptionsResponse
{
    public IReadOnlyList<SupplierOptionDto> Items { get; init; } = [];
}

/// <summary>
/// Selector ligero: solo `supplierId` y `businessName` de proveedores activos
/// (`REQ-PROVEEDORES-09`, `D-101`).
/// </summary>
public sealed class GetSupplierOptionsQueryHandler(
    SupplierAccessService supplierAccessService,
    ISupplierRepository supplierRepository)
{
    public const int MaxLimit = 100;

    public async Task<GetSupplierOptionsResponse> ExecuteAsync(
        Guid actorUserId,
        Guid? companyId,
        GetSupplierOptionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var scope = await supplierAccessService.EnsureReadAsync(actorUserId, companyId, cancellationToken);

        var filter = new SupplierFilter(
            scope.CompanyId,
            query.BusinessName,
            query.ContactName,
            query.CountryId,
            query.DepartmentId,
            query.MunicipalityId);

        var limit = query.Limit is > 0 and <= MaxLimit ? query.Limit : 50;

        var items = await supplierRepository.ListActiveOptionsAsync(filter, limit, cancellationToken);

        return new GetSupplierOptionsResponse
        {
            Items = items.Select(SupplierMapper.ToOptionDto).ToList()
        };
    }
}
