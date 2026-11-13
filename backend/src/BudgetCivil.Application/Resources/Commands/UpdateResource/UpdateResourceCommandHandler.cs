using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Dtos;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using BudgetCivil.Application.Suppliers.Repositories;
using BudgetCivil.Domain.Resources;
using FluentValidation;

namespace BudgetCivil.Application.Resources.Commands.UpdateResource;

public sealed class UpdateResourceCommand
{
    public Guid ResourceTypeId { get; init; }

    public Guid MunicipalityId { get; init; }

    public string Description { get; init; } = string.Empty;

    public string? Unit { get; init; }

    public decimal? UnitPrice { get; init; }

    public string? Brand { get; init; }

    public string? Distributor { get; init; }

    public string? UserKey { get; init; }

    public string? Details { get; init; }

    public string? ContactNumber { get; init; }

    public Guid? SupplierId { get; init; }

    public string RowVersion { get; init; } = string.Empty;
}

public sealed class UpdateResourceCommandValidator : AbstractValidator<UpdateResourceCommand>
{
    public UpdateResourceCommandValidator()
    {
        RuleFor(command => command.ResourceTypeId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.MunicipalityId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.Description)
            .NotEmpty()
            .MaximumLength(Resource.DescriptionMaxLength);

        RuleFor(command => command.Unit)
            .MaximumLength(Resource.UnitMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Unit));

        RuleFor(command => command.UnitPrice)
            .GreaterThanOrEqualTo(0m)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .When(command => command.UnitPrice.HasValue);

        RuleFor(command => command.Brand)
            .MaximumLength(Resource.BrandMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Brand));

        RuleFor(command => command.Distributor)
            .MaximumLength(Resource.DistributorMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Distributor));

        RuleFor(command => command.UserKey)
            .MaximumLength(Resource.UserKeyMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.UserKey));

        RuleFor(command => command.ContactNumber)
            .MaximumLength(Resource.ContactNumberMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.ContactNumber));

        RuleFor(command => command.RowVersion)
            .NotEmpty();

        RuleFor(command => command.SupplierId)
            .NotEqual(Guid.Empty)
            .When(command => command.SupplierId.HasValue);
    }
}

public sealed class UpdateResourceCommandHandler(
    ResourceAccessService resourceAccessService,
    IResourceRepository resourceRepository,
    ISupplierRepository supplierRepository,
    IValidator<UpdateResourceCommand> validator)
{
    public async Task<ResourceDto> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        Guid resourceId,
        UpdateResourceCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        var (_, access) = await resourceAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        var resource = await resourceRepository.GetByIdAsync(projectId, resourceId, cancellationToken)
            ?? throw new NotFoundAppException("Resource was not found.", "RESOURCE_NOT_FOUND");

        EnsureVersionMatches(command.RowVersion, resource);

        await EnsureReferencesAsync(command, cancellationToken);

        await ResourceSupplierRules.EnsureAssignableAsync(
            supplierRepository,
            command.SupplierId,
            access.CompanyId,
            resource.SupplierId,
            cancellationToken);

        try
        {
            resource.Update(
                command.ResourceTypeId,
                command.MunicipalityId,
                command.Description,
                command.Unit,
                command.UnitPrice,
                command.Brand,
                command.Distributor,
                command.UserKey,
                command.Details,
                command.ContactNumber,
                command.SupplierId,
                actorUserId,
                DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, "VALIDATION_ERROR");
        }

        await resourceRepository.SaveChangesAsync(cancellationToken);

        var stored = await resourceRepository.GetByIdAsync(projectId, resource.Id, cancellationToken)
            ?? throw new NotFoundAppException("Resource was not found after update.", "RESOURCE_NOT_FOUND");

        return ResourceMapper.ToDto(stored);
    }

    private static void EnsureVersionMatches(string rowVersion, Resource resource)
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

        if (!expectedVersion.SequenceEqual(resource.RowVersion))
        {
            throw new ConflictAppException(
                "Resource was modified by another user.",
                "RESOURCE_CONCURRENCY_CONFLICT");
        }
    }

    private async Task EnsureReferencesAsync(UpdateResourceCommand command, CancellationToken cancellationToken)
    {
        if (!await resourceRepository.ResourceTypeExistsAsync(command.ResourceTypeId, cancellationToken))
        {
            throw new ValidationAppException("Resource type is invalid.", "RESOURCE_TYPE_INVALID");
        }

        if (!await resourceRepository.MunicipalityExistsAsync(command.MunicipalityId, cancellationToken))
        {
            throw new ValidationAppException("Municipality is invalid.", "MUNICIPALITY_INVALID");
        }
    }

    private async Task ValidateAsync(UpdateResourceCommand command, CancellationToken cancellationToken)
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
