using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Catalogs;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Users.Bootstrap.Dtos;
using BudgetCivil.Application.Users.Bootstrap.Repositories;
using FluentValidation;

namespace BudgetCivil.Application.Users.Bootstrap.UseCases;

/// <summary>
/// `REQ-IDENTIDAD-27` (`D-110`): crea el primer Administrador, **sin compañía** y sin crear ninguna
/// empresa (`SC-IDENTIDAD-69`). El endpoint se cierra al existir un Administrador.
/// </summary>
public sealed class BootstrapFirstAdministratorUseCase(
    IBootstrapRepository bootstrapRepository,
    IGeographicCatalogRepository geographicCatalogRepository,
    IValidator<BootstrapRequest> validator)
{
    public async Task<BootstrapResponse> ExecuteAsync(BootstrapRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        if (request.AdminProfile.HasCompleteResidence)
        {
            await GeographicLocationRules.EnsureValidActiveLocationAsync(
                geographicCatalogRepository,
                request.AdminProfile.CountryId!.Value,
                request.AdminProfile.DepartmentId!.Value,
                request.AdminProfile.MunicipalityId!.Value,
                cancellationToken);
        }

        if (await bootstrapRepository.AnyAdministratorExistsAsync(cancellationToken))
        {
            throw new ConflictAppException("Bootstrap is disabled because an administrator already exists.", "BOOTSTRAP_ALREADY_COMPLETED");
        }

        if (await bootstrapRepository.EmailExistsAsync(request.AdminProfile.Email, cancellationToken))
        {
            throw new ConflictAppException("Email already exists.", "EMAIL_ALREADY_EXISTS");
        }

        if (await bootstrapRepository.UsernameExistsAsync(request.AdminProfile.Username, cancellationToken))
        {
            throw new ConflictAppException("Username already exists.", "USERNAME_ALREADY_EXISTS");
        }

        var creationResult = await bootstrapRepository.CreateFirstAdministratorAsync(request, cancellationToken);

        return new BootstrapResponse
        {
            BootstrapEnabled = false,
            AdministratorCreated = true,
            AdministratorId = creationResult.AdministratorId,
            AuditEventId = creationResult.AuditEventId
        };
    }

    private async Task ValidateAsync(BootstrapRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid)
        {
            return;
        }

        var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
        throw new ValidationAppException(message, "VALIDATION_ERROR");
    }
}
