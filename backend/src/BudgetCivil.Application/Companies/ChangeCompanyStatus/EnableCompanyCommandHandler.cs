using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Companies.Dtos;
using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Application.Companies.UseCases;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Companies.ChangeCompanyStatus;

public sealed class EnableCompanyCommandHandler(
    ICompanyRepository companyRepository,
    ISecurityAuditService securityAuditService)
{
    public async Task<DeactivateCompanyResponse> ExecuteAsync(
        Guid actorUserId,
        Guid companyId,
        EnableCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = await companyRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = CompanyAdministrationAuthorizationRules.EnsureActorCanReactivateCompany(actor);

        var company = await companyRepository.GetCompanyByIdAsync(companyId, cancellationToken);
        if (company is null)
        {
            throw new ConflictAppException("Company was not found.", "COMPANY_NOT_FOUND");
        }

        if (company.IsActive)
        {
            throw new ConflictAppException("Company is already active.", "COMPANY_ALREADY_ACTIVE");
        }

        company.IsActive = true;
        company.UpdatedAtUtc = DateTime.UtcNow;

        await companyRepository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                actor.CompanyId,
                SecurityAuditEventType.CompanyAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "COMPANIES_REACTIVATE",
                TargetType: nameof(Company),
                TargetId: company.Id,
                ReasonCode: "COMPANY_REACTIVATED",
                Metadata: new Dictionary<string, string>
                {
                    ["reason"] = command.Reason?.Trim() ?? string.Empty
                }),
            cancellationToken);

        return new DeactivateCompanyResponse
        {
            CompanyId = company.Id,
            IsActive = true
        };
    }
}
