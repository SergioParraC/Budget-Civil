using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Companies.Dtos;
using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Application.Companies.UseCases;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;
using CompanyEntity = BudgetCivil.Domain.Users.Entities.Company;
using CompanyDomainRules = BudgetCivil.Domain.Companies.Company;

namespace BudgetCivil.Application.Companies.UpdateCompany;

public sealed class UpdateCompanyCommandHandler(
    ICompanyRepository companyRepository,
    ISecurityAuditService securityAuditService,
    IValidator<UpdateCompanyCommand> validator)
{
    public async Task<UpdateCompanyResponse> ExecuteAsync(
        Guid actorUserId,
        Guid companyId,
        UpdateCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);

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

        if (await companyRepository.TaxIdExistsAsync(command.TaxId, companyId, cancellationToken))
        {
            throw new ConflictAppException("Company tax id already exists.", "COMPANY_TAX_ID_ALREADY_EXISTS");
        }

        var normalizedBusinessName = CompanyDomainRules.NormalizeBusinessName(command.BusinessName);
        var normalizedTaxId = CompanyDomainRules.NormalizeTaxId(command.TaxId);
        CompanyDomainRules.ValidateTerritorialLevel(command.CompanyType, command.TerritorialLevel);
        CompanyDomainRules.ValidateRepresentative(MapRepresentative(command.Representative));

        company.BusinessName = normalizedBusinessName;
        company.TaxId = normalizedTaxId;
        company.UpdatedAtUtc = DateTime.UtcNow;

        await companyRepository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                actor.CompanyId,
                SecurityAuditEventType.CompanyAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "COMPANIES_UPDATE",
                TargetType: nameof(CompanyEntity),
                TargetId: company.Id,
                ReasonCode: "COMPANY_UPDATED"),
            cancellationToken);

        return new UpdateCompanyResponse
        {
            CompanyId = company.Id,
            UpdatedAtUtc = company.UpdatedAtUtc
        };
    }

    private static BudgetCivil.Domain.Companies.LegalRepresentativeInput? MapRepresentative(LegalRepresentativePayload? representative)
    {
        if (representative is null)
        {
            return null;
        }

        return new BudgetCivil.Domain.Companies.LegalRepresentativeInput
        {
            Mode = representative.Mode,
            UserId = representative.UserId,
            FullName = representative.FullName,
            DocumentNumber = representative.DocumentNumber
        };
    }

    private async Task ValidateAsync(UpdateCompanyCommand command, CancellationToken cancellationToken)
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
