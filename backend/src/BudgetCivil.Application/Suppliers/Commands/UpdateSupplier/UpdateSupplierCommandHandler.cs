using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Suppliers.Dtos;
using BudgetCivil.Application.Suppliers.Repositories;
using BudgetCivil.Application.Suppliers.Services;
using BudgetCivil.Domain.Suppliers;
using FluentValidation;

namespace BudgetCivil.Application.Suppliers.Commands.UpdateSupplier;

public sealed class UpdateSupplierCommand
{
    public string BusinessName { get; init; } = string.Empty;

    public string? ContactName { get; init; }

    public string? ContactPhone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    public string? Observations { get; init; }

    public Guid? CountryId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? MunicipalityId { get; init; }

    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(command => command.BusinessName)
            .NotEmpty()
            .MaximumLength(Supplier.BusinessNameMaxLength);

        RuleFor(command => command.ContactName)
            .MaximumLength(Supplier.ContactNameMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.ContactName));

        RuleFor(command => command.ContactPhone)
            .MaximumLength(Supplier.ContactPhoneMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.ContactPhone));

        RuleFor(command => command.Email)
            .MaximumLength(Supplier.EmailMaxLength)
            .EmailAddress()
            .When(command => !string.IsNullOrWhiteSpace(command.Email));

        RuleFor(command => command.Address)
            .MaximumLength(Supplier.AddressMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Address));

        RuleFor(command => command.Observations)
            .MaximumLength(Supplier.ObservationsMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Observations));

        RuleFor(command => command.CountryId).NotEqual(Guid.Empty).When(command => command.CountryId.HasValue);
        RuleFor(command => command.DepartmentId).NotEqual(Guid.Empty).When(command => command.DepartmentId.HasValue);
        RuleFor(command => command.MunicipalityId).NotEqual(Guid.Empty).When(command => command.MunicipalityId.HasValue);

        RuleFor(command => command.RowVersion).NotEmpty();
    }
}

public sealed class UpdateSupplierCommandHandler(
    SupplierAccessService supplierAccessService,
    ISupplierRepository supplierRepository,
    IValidator<UpdateSupplierCommand> validator)
{
    public async Task<SupplierDto> ExecuteAsync(
        Guid actorUserId,
        Guid supplierId,
        UpdateSupplierCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var (_, companyId) = await supplierAccessService.EnsureWriteAsync(actorUserId, cancellationToken);

        var supplier = await supplierRepository.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new NotFoundAppException("Supplier was not found.", "SUPPLIER_NOT_FOUND");

        SupplierAccessService.EnsureCompanyScope(supplier.CompanyId, companyId);

        EnsureVersionMatches(command.RowVersion, supplier);

        await SupplierLocationRules.EnsureLocationAsync(
            supplierRepository,
            command.CountryId,
            command.DepartmentId,
            command.MunicipalityId,
            cancellationToken);

        try
        {
            supplier.Update(
                command.BusinessName,
                command.ContactName,
                command.ContactPhone,
                command.Email,
                command.Address,
                command.Observations,
                command.CountryId,
                command.DepartmentId,
                command.MunicipalityId,
                actorUserId,
                DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, "VALIDATION_ERROR");
        }

        await supplierRepository.SaveChangesAsync(cancellationToken);

        var stored = await supplierRepository.GetByIdAsync(supplier.Id, cancellationToken)
            ?? throw new NotFoundAppException("Supplier was not found after update.", "SUPPLIER_NOT_FOUND");

        return SupplierMapper.ToDto(stored);
    }

    private static void EnsureVersionMatches(string rowVersion, Supplier supplier)
    {
        byte[] expectedVersion;
        try
        {
            expectedVersion = Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            throw new ValidationAppException("rowVersion is invalid.", "VALIDATION_ERROR");
        }

        if (!expectedVersion.SequenceEqual(supplier.RowVersion))
        {
            throw new ConflictAppException(
                "Supplier was modified by another user.",
                "SUPPLIER_CONCURRENCY_CONFLICT");
        }
    }

    private async Task ValidateAsync(UpdateSupplierCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (validationResult.IsValid)
        {
            return;
        }

        var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
        throw new ValidationAppException(message, "VALIDATION_ERROR");
    }
}
