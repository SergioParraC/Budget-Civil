using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Companies.Dtos;
using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Application.Companies.UseCases;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Companies.ChangeCompanyStatus;

public sealed class DisableCompanyCommandHandler(
    ICompanyRepository companyRepository,
    ISecurityAuditService securityAuditService)
{
    public async Task<DeactivateCompanyResponse> ExecuteAsync(
        Guid actorUserId,
        Guid companyId,
        DisableCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        var reason = command.GetNormalizedReason();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationAppException("reason is required.", "VALIDATION_ERROR");
        }

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
                    ["reason"] = reason
                }),
            cancellationToken);

        return new DeactivateCompanyResponse
        {
            CompanyId = company.Id,
            IsActive = false
        };
    }
}
