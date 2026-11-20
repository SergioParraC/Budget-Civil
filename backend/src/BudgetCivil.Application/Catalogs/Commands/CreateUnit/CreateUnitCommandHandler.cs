using BudgetCivil.Application.Catalogs.Dtos;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Catalogs.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Domain.Catalogs;
using FluentValidation;

namespace BudgetCivil.Application.Catalogs.Commands.CreateUnit;

public sealed class CreateUnitCommand
{
    public string ShortUnit { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;
}

public sealed class CreateUnitCommandValidator : AbstractValidator<CreateUnitCommand>
{
    public CreateUnitCommandValidator()
    {
        RuleFor(command => command.ShortUnit)
            .NotEmpty()
            .MaximumLength(MeasurementUnit.ShortUnitMaxLength);

        RuleFor(command => command.Unit)
            .NotEmpty()
            .MaximumLength(MeasurementUnit.UnitMaxLength);
    }
}

public sealed class CreateUnitCommandHandler(
    IMeasurementUnitRepository repository,
    IValidator<CreateUnitCommand> validator)
{
    public async Task<UnitDto> ExecuteAsync(
        CreateUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);

        MeasurementUnit unit;
        try
        {
            unit = MeasurementUnit.Create(command.ShortUnit, command.Unit);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationAppException(exception.Message, UnitRules.ValidationError);
        }

        if (await repository.ShortUnitExistsAsync(unit.ShortUnit, cancellationToken))
        {
            throw new ConflictAppException("Measurement unit already exists.", UnitRules.AlreadyExists);
        }

        await repository.AddAsync(unit, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new UnitDto
        {
            Id = unit.Id,
            ShortUnit = unit.ShortUnit,
            Unit = unit.Unit
        };
    }

    private async Task ValidateAsync(CreateUnitCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (validationResult.IsValid)
        {
            return;
        }

        var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
        throw new ValidationAppException(message, UnitRules.ValidationError);
    }
}