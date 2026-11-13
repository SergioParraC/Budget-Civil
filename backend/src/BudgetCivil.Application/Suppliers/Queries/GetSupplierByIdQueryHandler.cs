using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Suppliers.Dtos;
using BudgetCivil.Application.Suppliers.Repositories;
using BudgetCivil.Application.Suppliers.Services;

namespace BudgetCivil.Application.Suppliers.Queries;

/// <summary>Detalle completo del proveedor, con ubicación y observaciones (`REQ-PROVEEDORES-08`).</summary>
public sealed class GetSupplierByIdQueryHandler(
    SupplierAccessService supplierAccessService,
    ISupplierRepository supplierRepository)
{
    public async Task<SupplierDto> ExecuteAsync(
        Guid actorUserId,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        var scope = await supplierAccessService.EnsureReadAsync(actorUserId, null, cancellationToken);

        var supplier = await supplierRepository.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new NotFoundAppException("Supplier was not found.", "SUPPLIER_NOT_FOUND");

        SupplierAccessService.EnsureCompanyScope(supplier.CompanyId, scope.CompanyId);

        return SupplierMapper.ToDto(supplier);
    }
}
