using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Companies.Dtos;
using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Companies.UseCases;

public sealed class DeactivateCompanyUseCase(
    ICompanyRepository companyRepository,
    ISecurityAuditService securityAuditService,
    IValidator<DeactivateCompanyRequest> validator)
{
    public async Task<DeactivateCompanyResponse> ExecuteAsync(
        Guid actorUserId,
        Guid companyId,
        DeactivateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var actor = await companyRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = CompanyAdministrationAuthorizationRules.EnsureActorCanDeactivateCompany(actor);

        var company = await companyRepository.GetCompanyByIdAsync(companyId, cancellationToken);
        if (company is null)
        {
            throw new ConflictAppException("Company was not found.", "COMPANY_NOT_FOUND");
        }

        if (!company.IsActive)
        {
            throw new ConflictAppException("Company is already inactive.", "COMPANY_ALREADY_INACTIVE");
        }

        company.IsActive = false;
        company.LastModifiedByUserId = actor.Id;
        company.LastModifiedAtUtc = DateTime.UtcNow;

        await companyRepository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                actor.CompanyId,
                SecurityAuditEventType.CompanyAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "COMPANIES_DEACTIVATE",
                TargetType: nameof(Company),
                TargetId: company.Id,
                ReasonCode: "COMPANY_DEACTIVATED",
                Metadata: new Dictionary<string, string>
                {
                    ["reason"] = request.Reason.Trim()
                }),
            cancellationToken);

        return new DeactivateCompanyResponse
        {
            CompanyId = company.Id,
            IsActive = company.IsActive
        };
    }

    private async Task ValidateAsync(DeactivateCompanyRequest request, CancellationToken cancellationToken)
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
