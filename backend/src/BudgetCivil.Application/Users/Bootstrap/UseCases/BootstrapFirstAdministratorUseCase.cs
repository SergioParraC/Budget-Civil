using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Users.Bootstrap.Dtos;
using BudgetCivil.Application.Users.Bootstrap.Repositories;
using FluentValidation;

namespace BudgetCivil.Application.Users.Bootstrap.UseCases;

public sealed class BootstrapFirstAdministratorUseCase(
    IBootstrapRepository bootstrapRepository,
    IValidator<BootstrapRequest> validator)
{
    public async Task<BootstrapResponse> ExecuteAsync(BootstrapRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

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

        if (await bootstrapRepository.CompanyTaxIdExistsAsync(request.Company.TaxId, cancellationToken))
        {
            throw new ConflictAppException("Company tax id already exists.", "COMPANY_TAX_ID_ALREADY_EXISTS");
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
