using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Companies.Dtos;
using BudgetCivil.Application.Companies;
using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Application.Companies.UseCases;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;
using CompanyEntity = BudgetCivil.Domain.Users.Entities.Company;
using CompanyDomainRules = BudgetCivil.Domain.Companies.Company;

namespace BudgetCivil.Application.Companies.CreateCompany;

public sealed class CreateCompanyCommandHandler(
    ICompanyRepository companyRepository,
    IGeographicCatalogRepository geographicCatalogRepository,
    ISecurityAuditService securityAuditService,
    IValidator<CreateCompanyCommand> validator)
{
    public async Task<CreateCompanyResponse> ExecuteAsync(
        Guid actorUserId,
        CreateCompanyCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);
        await CompanyReferenceRules.EnsureValidReferencesAsync(
            geographicCatalogRepository,
            command.CountryId,
            command.DepartmentId,
            command.MunicipalityId,
            command.Representative,
            cancellationToken);

        var actor = await companyRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = CompanyAdministrationAuthorizationRules.EnsureActorCanCreateCompany(actor);

        if (await companyRepository.TaxIdExistsAsync(command.TaxId, cancellationToken: cancellationToken))
        {
            throw new ConflictAppException("Company tax id already exists.", "COMPANY_TAX_ID_ALREADY_EXISTS");
        }

        var isManager = actor.Role.Code == CompanyAdministrationAuthorizationRules.DirectorRoleCode;
        if (isManager && actor.CompanyId.HasValue)
        {
            throw new ConflictAppException(
                "Director de proyectos can only create one own company.",
                "MANAGER_COMPANY_LIMIT_REACHED");
        }

        var normalizedBusinessName = CompanyDomainRules.NormalizeBusinessName(command.BusinessName);
        var normalizedTaxId = CompanyDomainRules.NormalizeTaxId(command.TaxId);
        var normalizedCompanyType = CompanyDomainRules.NormalizeCompanyType(command.CompanyType);
        CompanyDomainRules.ValidateTerritorialLevel(command.CompanyType, command.TerritorialLevel);
        CompanyDomainRules.ValidateRepresentative(MapRepresentative(command.Representative));

        var utcNow = DateTime.UtcNow;
        var createdCompany = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            BusinessName = normalizedBusinessName,
            TaxId = normalizedTaxId,
            CompanyType = normalizedCompanyType,
            TerritorialLevel = NormalizeOptionalText(command.TerritorialLevel),
            CountryId = command.CountryId,
            DepartmentId = command.DepartmentId,
            MunicipalityId = command.MunicipalityId,
            RepresentativeMode = command.Representative!.Mode!.Trim().ToUpperInvariant(),
            RepresentativeUserId = command.Representative.UserId,
            RepresentativeFullName = NormalizeOptionalText(command.Representative.FullName),
            RepresentativeDocumentTypeId = command.Representative.DocumentTypeId,
            RepresentativeDocumentNumber = NormalizeOptionalText(command.Representative.DocumentNumber),
            IsActive = isManager ? true : command.IsActive ?? true,
            CreatedByUserId = actor.Id,
            CreatedAtUtc = utcNow,
        };

        await companyRepository.AddCompanyAsync(createdCompany, cancellationToken);

        if (isManager)
        {
            // D-98: la pertenencia a la compañía vive en User.company_id.
            actor.CompanyId = createdCompany.Id;
            actor.UpdatedAtUtc = utcNow;
        }

        await companyRepository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                actor.CompanyId,
                SecurityAuditEventType.CompanyAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "COMPANIES_CREATE",
                TargetType: nameof(CompanyEntity),
                TargetId: createdCompany.Id,
                ReasonCode: "COMPANY_CREATED",
                Metadata: new Dictionary<string, string>
                {
                    ["actor_role_code"] = actor.Role.Code
                }),
            cancellationToken);

        return new CreateCompanyResponse
        {
            CompanyId = createdCompany.Id,
            CreatedAtUtc = createdCompany.CreatedAtUtc
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
            DocumentNumber = representative.DocumentNumber,
            DocumentTypeId = representative.DocumentTypeId
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task ValidateAsync(CreateCompanyCommand command, CancellationToken cancellationToken)
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
