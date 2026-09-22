using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Companies.Dtos;
using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Companies.UseCases;

public sealed class UpdateCompanyUseCase(
    ICompanyRepository companyRepository,
    ISecurityAuditService securityAuditService,
    IValidator<UpdateCompanyRequest> validator)
{
    public async Task<UpdateCompanyResponse> ExecuteAsync(
        Guid actorUserId,
        Guid companyId,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var actor = await companyRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = CompanyAdministrationAuthorizationRules.EnsureActorCanUpdateCompany(actor, companyId);

        var company = await companyRepository.GetCompanyByIdAsync(companyId, cancellationToken);
        if (company is null)
        {
            throw new ConflictAppException("Company was not found.", "COMPANY_NOT_FOUND");
        }

        if (!company.IsActive)
        {
            throw new ConflictAppException("Company is inactive.", "COMPANY_INACTIVE");
        }

        if (await companyRepository.TaxIdExistsAsync(request.TaxId, companyId, cancellationToken))
        {
            throw new ConflictAppException("Company tax id already exists.", "COMPANY_TAX_ID_ALREADY_EXISTS");
        }

        company.BusinessName = request.BusinessName.Trim();
        company.TaxId = request.TaxId.Trim();
        company.UpdatedAtUtc = DateTime.UtcNow;

        await companyRepository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                actor.CompanyId,
                SecurityAuditEventType.CompanyAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "COMPANIES_UPDATE",
                TargetType: nameof(Company),
                TargetId: company.Id,
                ReasonCode: "COMPANY_UPDATED"),
            cancellationToken);

        return new UpdateCompanyResponse
        {
            CompanyId = company.Id,
            UpdatedAtUtc = company.UpdatedAtUtc
        };
    }

    private async Task ValidateAsync(UpdateCompanyRequest request, CancellationToken cancellationToken)
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
