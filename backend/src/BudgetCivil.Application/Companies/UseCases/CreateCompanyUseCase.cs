using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Companies.Dtos;
using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Companies.UseCases;

public sealed class CreateCompanyUseCase(
    ICompanyRepository companyRepository,
    ISecurityAuditService securityAuditService,
    IValidator<CreateCompanyRequest> validator)
{
    public async Task<CreateCompanyResponse> ExecuteAsync(
        Guid actorUserId,
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var actor = await companyRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = CompanyAdministrationAuthorizationRules.EnsureActorCanCreateCompany(actor);

        if (await companyRepository.TaxIdExistsAsync(request.TaxId, cancellationToken: cancellationToken))
        {
            throw new ConflictAppException("Company tax id already exists.", "COMPANY_TAX_ID_ALREADY_EXISTS");
        }

        var isManager = actor.Role.Code == CompanyAdministrationAuthorizationRules.ProjectManagerRoleCode;
        if (isManager)
        {
            var managerActiveMembership = await companyRepository.GetActiveMembershipByUserIdAsync(actor.Id, cancellationToken);
            if (actor.CompanyId.HasValue || managerActiveMembership is not null)
            {
                throw new ConflictAppException(
                    "Gerente de proyectos can only create one own company.",
                    "MANAGER_COMPANY_LIMIT_REACHED");
            }
        }

        var utcNow = DateTime.UtcNow;
        var createdCompany = new Company
        {
            Id = Guid.NewGuid(),
            BusinessName = request.BusinessName.Trim(),
            TaxId = request.TaxId.Trim(),
            IsActive = isManager ? true : request.IsActive ?? true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        await companyRepository.AddCompanyAsync(createdCompany, cancellationToken);

        if (isManager)
        {
            actor.CompanyId = createdCompany.Id;
            actor.UpdatedAtUtc = utcNow;

            await companyRepository.AddMembershipAsync(
                new UserCompanyMembership
                {
                    Id = Guid.NewGuid(),
                    UserId = actor.Id,
                    CompanyId = createdCompany.Id,
                    IsActive = true,
                    AssignedByUserId = actor.Id,
                    AssignedAtUtc = utcNow
                },
                cancellationToken);
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
}
