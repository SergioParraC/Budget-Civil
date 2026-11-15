using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Repositories;
using BudgetCivil.Application.Apus.Services;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Resources;
using FluentValidation;

namespace BudgetCivil.Application.Apus.Commands.MatrixComponents;

public sealed class AddMatrixComponentCommand
{
    public Guid ComponentResourceId { get; init; }

    public decimal Consumption { get; init; }

    public decimal WastePercentage { get; init; }

    public string? RowVersion { get; init; }
}

public sealed class UpdateMatrixComponentCommand
{
    public decimal Consumption { get; init; }

    public decimal WastePercentage { get; init; }

    public string? RowVersion { get; init; }
}

public sealed class RemoveMatrixComponentCommand
{
    public string? RowVersion { get; init; }
}

public sealed class AddMatrixComponentCommandValidator : AbstractValidator<AddMatrixComponentCommand>
{
    public AddMatrixComponentCommandValidator()
    {
        RuleFor(command => command.ComponentResourceId)
            .NotEqual(Guid.Empty);

        RuleFor(command => command.Consumption)
            .GreaterThan(0m);

        RuleFor(command => command.WastePercentage)
            .InclusiveBetween(0m, 100m);

        RuleFor(command => command.RowVersion)
            .NotEmpty();
    }
}

public sealed class UpdateMatrixComponentCommandValidator : AbstractValidator<UpdateMatrixComponentCommand>
{
    public UpdateMatrixComponentCommandValidator()
    {
        RuleFor(command => command.Consumption)
            .GreaterThan(0m);

        RuleFor(command => command.WastePercentage)
            .InclusiveBetween(0m, 100m);

        RuleFor(command => command.RowVersion)
            .NotEmpty();
    }
}

public sealed class RemoveMatrixComponentCommandValidator : AbstractValidator<RemoveMatrixComponentCommand>
{
    public RemoveMatrixComponentCommandValidator()
    {
        RuleFor(command => command.RowVersion)
            .NotEmpty();
    }
}

internal static class MatrixComponentRules
{
    public static async Task<Resource> EnsureMatrixAsync(
        IApuRepository apuRepository,
        Guid projectId,
        Guid matrixResourceId,
        string? rowVersion,
        CancellationToken cancellationToken)
    {
        var matrix = await apuRepository.GetProjectResourceAsync(projectId, matrixResourceId, cancellationToken)
            ?? throw new NotFoundAppException("La Matriz no existe en el proyecto.", ApuRules.MatrixNotFound);

        if (!string.Equals(matrix.ResourceType?.Code, ResourceTypeCatalog.Matrix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationAppException("El insumo no es de tipo Matriz.", ApuRules.MatrixTypeInvalid);
        }

        var requestedRowVersion = ApuRules.ParseRowVersion(rowVersion);
        ApuRules.EnsureMatrixRowVersion(matrix.RowVersion, requestedRowVersion);
        apuRepository.SetOriginalRowVersion(matrix, requestedRowVersion);
        return matrix;
    }

    public static async Task<Resource> EnsureComponentAsync(
        IApuRepository apuRepository,
        Guid projectId,
        Guid matrixResourceId,
        Guid componentResourceId,
        CancellationToken cancellationToken)
    {
        var component = await apuRepository.GetProjectResourceAsync(
            projectId,
            componentResourceId,
            cancellationToken)
            ?? throw new NotFoundAppException(
                "El insumo componente no existe en el proyecto.",
                ApuRules.ApuResourceNotFound);

        if (componentResourceId == matrixResourceId)
        {
            throw new ValidationAppException(
                "Una Matriz no puede contenerse a sí misma.",
                ApuRules.ValidationError);
        }

        if (string.Equals(component.ResourceType?.Code, ResourceTypeCatalog.Matrix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationAppException(
                "Una Matriz no puede contener otra Matriz.",
                ApuRules.MatrixNestingForbidden);
        }

        return component;
    }

    public static async Task PersistAsync(
        IApuRepository apuRepository,
        IMatrixApuStatusUpdater matrixApuStatusUpdater,
        Guid projectId,
        Guid matrixResourceId,
        Guid actorUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        // Primero se persiste la composición para que el conteo y el insumo reflejen el cambio; después se
        // actualiza la autoría y el estado de la Matriz y se recalculan los APUs que la usan (`D4`).
        await apuRepository.SaveChangesAsync(cancellationToken);
        await apuRepository.MarkMatrixChangedAsync(matrixResourceId, actorUserId, utcNow, cancellationToken);
        await matrixApuStatusUpdater.RecalculateForMatrixAsync(projectId, matrixResourceId, cancellationToken);
        await apuRepository.SaveChangesAsync(cancellationToken);
    }
}
