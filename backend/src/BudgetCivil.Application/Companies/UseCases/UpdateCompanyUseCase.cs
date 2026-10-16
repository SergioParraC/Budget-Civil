using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Companies;
using BudgetCivil.Application.Companies.Dtos;
using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Companies.UseCases;

public sealed class UpdateCompanyUseCase(
    ICompanyRepository companyRepository,
    IGeographicCatalogRepository geographicCatalogRepository,
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
        await CompanyReferenceRules.EnsureValidReferencesAsync(
            geographicCatalogRepository,
            request.CountryId,
            request.DepartmentId,
            request.MunicipalityId,
            request.Representative,
            cancellationToken);
        BudgetCivil.Domain.Companies.Company.ValidateTerritorialLevel(request.CompanyType, request.TerritorialLevel);
        BudgetCivil.Domain.Companies.Company.ValidateRepresentative(MapRepresentative(request.Representative));

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
        company.CompanyType = BudgetCivil.Domain.Companies.Company.NormalizeCompanyType(request.CompanyType);
        company.TerritorialLevel = NormalizeOptionalText(request.TerritorialLevel);
        company.CountryId = request.CountryId;
        company.DepartmentId = request.DepartmentId;
        company.MunicipalityId = request.MunicipalityId;
        company.RepresentativeMode = request.Representative!.Mode!.Trim().ToUpperInvariant();
        company.RepresentativeUserId = request.Representative.UserId;
        company.RepresentativeFullName = NormalizeOptionalText(request.Representative.FullName);
        company.RepresentativeDocumentTypeId = request.Representative.DocumentTypeId;
        company.RepresentativeDocumentNumber = NormalizeOptionalText(request.Representative.DocumentNumber);
        company.LastModifiedByUserId = actor.Id;
        company.LastModifiedAtUtc = DateTime.UtcNow;

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
            UpdatedAtUtc = company.LastModifiedAtUtc!.Value
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

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static BudgetCivil.Domain.Companies.LegalRepresentativeInput? MapRepresentative(LegalRepresentativePayload? representative)
    {
        return representative is null
            ? null
            : new BudgetCivil.Domain.Companies.LegalRepresentativeInput
            {
                Mode = representative.Mode,
                UserId = representative.UserId,
                FullName = representative.FullName,
                DocumentNumber = representative.DocumentNumber,
                DocumentTypeId = representative.DocumentTypeId
            };
    }
}
