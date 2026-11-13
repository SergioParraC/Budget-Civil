using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Suppliers.Dtos;
using BudgetCivil.Application.Suppliers.Repositories;
using BudgetCivil.Application.Suppliers.Services;
using FluentValidation;

namespace BudgetCivil.Application.Suppliers.Commands.ChangeSupplierStatus;

/// <summary>Baja lógica y reactivación (`REQ-PROVEEDORES-11`, `D-49`).</summary>
public sealed class ChangeSupplierStatusCommand
{
    public bool IsActive { get; init; }
}

public sealed class ChangeSupplierStatusCommandValidator : AbstractValidator<ChangeSupplierStatusCommand>
{
    public ChangeSupplierStatusCommandValidator()
    {
        RuleFor(command => command.IsActive).NotNull();
    }
}

public sealed class ChangeSupplierStatusCommandHandler(
    SupplierAccessService supplierAccessService,
    ISupplierRepository supplierRepository,
    IValidator<ChangeSupplierStatusCommand> validator)
{
    public async Task<SupplierDto> ExecuteAsync(
        Guid actorUserId,
        Guid supplierId,
        ChangeSupplierStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
            throw new ValidationAppException(message, "VALIDATION_ERROR");
        }

        var (_, companyId) = await supplierAccessService.EnsureWriteAsync(actorUserId, cancellationToken);

        var supplier = await supplierRepository.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new NotFoundAppException("Supplier was not found.", "SUPPLIER_NOT_FOUND");

        SupplierAccessService.EnsureCompanyScope(supplier.CompanyId, companyId);

        var utcNow = DateTime.UtcNow;
        if (command.IsActive)
        {
            supplier.Activate(actorUserId, utcNow);
        }
        else
        {
            supplier.Deactivate(actorUserId, utcNow);
        }

        await supplierRepository.SaveChangesAsync(cancellationToken);

        var stored = await supplierRepository.GetByIdAsync(supplier.Id, cancellationToken)
            ?? throw new NotFoundAppException("Supplier was not found after update.", "SUPPLIER_NOT_FOUND");

        return SupplierMapper.ToDto(stored);
    }
}
