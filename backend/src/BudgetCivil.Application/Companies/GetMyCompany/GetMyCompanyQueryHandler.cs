using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Companies.GetMyCompany;

/// <summary>
/// Ficha de la compañía del actor autenticado para el dashboard. Devuelve <c>null</c> cuando el
/// usuario no tiene un vínculo vigente en <c>users_company</c>.
/// </summary>
public sealed class MyCompanyResponse
{
    public Guid CompanyId { get; init; }

    public string BusinessName { get; init; } = string.Empty;

    public string TaxId { get; init; } = string.Empty;

    public string? CompanyType { get; init; }

    public string? TerritorialLevel { get; init; }

    public Guid? CountryId { get; init; }

    public string? CountryName { get; init; }

    public Guid? DepartmentId { get; init; }

    public string? DepartmentName { get; init; }

    public Guid? MunicipalityId { get; init; }

    public string? MunicipalityName { get; init; }

    public string? RepresentativeMode { get; init; }

    public Guid? RepresentativeUserId { get; init; }

    public string? RepresentativeFullName { get; init; }

    public Guid? RepresentativeDocumentTypeId { get; init; }

    public string? RepresentativeDocumentNumber { get; init; }

    public bool IsActive { get; init; }

    /// <summary>Fecha de vinculación del actor con la compañía.</summary>
    public DateTime? LinkedAtUtc { get; init; }

    /// <summary>Rol del actor dentro de la compañía (rol de la cuenta).</summary>
    public string RoleCode { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;
}

public sealed class GetMyCompanyQueryHandler(ICompanyMembershipRepository repository)
{
    public async Task<MyCompanyResponse?> ExecuteAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken)
            ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!actor.AccountEnabled || actor.AccountLocked || !actor.Role.IsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        var membership = actor.ActiveMembership;
        if (membership is null)
        {
            return null;
        }

        var company = await repository.GetCompanyWithDetailsAsync(membership.CompanyId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        var representativeFullName = company.RepresentativeFullName;
        if (string.IsNullOrWhiteSpace(representativeFullName) && company.RepresentativeUserId.HasValue)
        {
            var representative = await repository.GetUserByIdAsync(company.RepresentativeUserId.Value, cancellationToken);
            representativeFullName = representative is null
                ? null
                : $"{representative.FirstName} {representative.LastName}".Trim();
        }

        return new MyCompanyResponse
        {
            CompanyId = company.Id,
            BusinessName = company.BusinessName,
            TaxId = company.TaxId,
            CompanyType = company.CompanyType,
            TerritorialLevel = company.TerritorialLevel,
            CountryId = company.CountryId,
            CountryName = company.Country?.Name,
            DepartmentId = company.DepartmentId,
            DepartmentName = company.Department?.Name,
            MunicipalityId = company.MunicipalityId,
            MunicipalityName = company.Municipality?.Name,
            RepresentativeMode = company.RepresentativeMode,
            RepresentativeUserId = company.RepresentativeUserId,
            RepresentativeFullName = representativeFullName,
            RepresentativeDocumentTypeId = company.RepresentativeDocumentTypeId,
            RepresentativeDocumentNumber = company.RepresentativeDocumentNumber,
            IsActive = company.IsActive,
            LinkedAtUtc = membership.CreatedAtUtc,
            RoleCode = actor.Role.Code,
            RoleName = actor.Role.Name
        };
    }
}
