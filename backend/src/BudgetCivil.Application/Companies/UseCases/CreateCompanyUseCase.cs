using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Companies;
using BudgetCivil.Application.Companies.Dtos;
using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Companies.UseCases;

public sealed class CreateCompanyUseCase(
    ICompanyRepository companyRepository,
    IGeographicCatalogRepository geographicCatalogRepository,
    ISecurityAuditService securityAuditService,
    IValidator<CreateCompanyRequest> validator)
{
    public async Task<CreateCompanyResponse> ExecuteAsync(
        Guid actorUserId,
        CreateCompanyRequest request,
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
        actor = CompanyAdministrationAuthorizationRules.EnsureActorCanCreateCompany(actor);

        if (await companyRepository.TaxIdExistsAsync(request.TaxId, cancellationToken: cancellationToken))
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

        var utcNow = DateTime.UtcNow;
        var createdCompany = new Company
        {
            Id = Guid.NewGuid(),
            BusinessName = request.BusinessName.Trim(),
            TaxId = request.TaxId.Trim(),
            CompanyType = BudgetCivil.Domain.Companies.Company.NormalizeCompanyType(request.CompanyType),
            TerritorialLevel = NormalizeOptionalText(request.TerritorialLevel),
            CountryId = request.CountryId,
            DepartmentId = request.DepartmentId,
            MunicipalityId = request.MunicipalityId,
            RepresentativeMode = request.Representative!.Mode!.Trim().ToUpperInvariant(),
            RepresentativeUserId = request.Representative.UserId,
            RepresentativeFullName = NormalizeOptionalText(request.Representative.FullName),
            RepresentativeDocumentTypeId = request.Representative.DocumentTypeId,
            RepresentativeDocumentNumber = NormalizeOptionalText(request.Representative.DocumentNumber),
            IsActive = isManager ? true : request.IsActive ?? true,
            CreatedByUserId = actor.Id,
            CreatedAtUtc = utcNow,
        };

        await companyRepository.AddCompanyAsync(createdCompany, cancellationToken);

        if (isManager)
        {
            // `users_company` es la única fuente de verdad de la pertenencia (`D-98` revisado).
            CompanyMembershipAssignment.Attach(actor, createdCompany.Id, utcNow);
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
                TargetType: nameof(Company),
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

    private async Task ValidateAsync(CreateCompanyRequest request, CancellationToken cancellationToken)
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
