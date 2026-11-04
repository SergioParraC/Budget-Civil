using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Dtos;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using BudgetCivil.Domain.Resources;
using FluentValidation;

namespace BudgetCivil.Application.Resources.Commands.CreateResource;

public sealed class CreateResourceCommand
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
}

public sealed class CreateResourceCommandValidator : AbstractValidator<CreateResourceCommand>
{
    public CreateResourceCommandValidator()
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
    }
}

public sealed class CreateResourceCommandHandler(
    ResourceAccessService resourceAccessService,
    IResourceRepository resourceRepository,
    IValidator<CreateResourceCommand> validator)
{
    public async Task<ResourceDto> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CreateResourceCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        await resourceAccessService.EnsureWriteAsync(actorUserId, projectId, cancellationToken);

        await EnsureReferencesAsync(command, cancellationToken);

        Resource resource;
        try
        {
            resource = Resource.Create(
                projectId,
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
                actorUserId,
                DateTime.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, "VALIDATION_ERROR");
        }

        await resourceRepository.AddAsync(resource, cancellationToken);
        await resourceRepository.SaveChangesAsync(cancellationToken);

        var stored = await resourceRepository.GetByIdAsync(projectId, resource.Id, cancellationToken)
            ?? throw new NotFoundAppException("Resource was not found after creation.", "RESOURCE_NOT_FOUND");

        return ResourceMapper.ToDto(stored);
    }

    private async Task EnsureReferencesAsync(CreateResourceCommand command, CancellationToken cancellationToken)
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

    private async Task ValidateAsync(CreateResourceCommand command, CancellationToken cancellationToken)
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
