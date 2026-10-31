using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Listings.Repositories;

namespace BudgetCivil.Application.Listings.Queries;

public enum ScopedUsersListingScope
{
    Global = 0,
    CompanyById = 1,
    MyCompany = 2,
    MyCompanyEstimatorsAuditors = 3
}

public sealed class GetScopedUsersQuery
{
    public ScopedUsersListingScope Scope { get; init; } = ScopedUsersListingScope.Global;

    public Guid? CompanyId { get; init; }

    public string? Search { get; init; }

    public string? RoleCode { get; init; }

    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public sealed class GetScopedUsersResponse
{
    public IReadOnlyList<ScopedUserListItem> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }
}

public sealed class ScopedUserListItem
{
    public Guid UserId { get; init; }

    public Guid? CompanyId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string RoleCode { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;

    public bool AccountEnabled { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public string? SecondLastName { get; init; }

    public string DocumentNumber { get; init; } = string.Empty;

    /// <summary>Fecha de vinculación con la compañía (`users_company.created_at`).</summary>
    public DateTime? LinkedAtUtc { get; init; }

    /// <summary>Última actualización del vínculo (reactivaciones incluidas).</summary>
    public DateTime? MembershipUpdatedAtUtc { get; init; }
}

public sealed class GetScopedUsersQueryHandler(
    IListingRepository listingRepository,
    ListingQueryPipeline listingQueryPipeline)
{
    private const string AdministratorRoleCode = "ADMINISTRADOR";
    private const string DirectorRoleCode = "DIRECTOR";
    private const string EstimatorRoleCode = "PRESUPUESTADOR";
    private const string AuditorRoleCode = "AUDITOR";

    public async Task<GetScopedUsersResponse> ExecuteAsync(
        Guid actorUserId,
        GetScopedUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        var actor = await listingRepository.GetActorSnapshotAsync(actorUserId, cancellationToken);
        var resolvedActor = EnsureActorOperational(actor);

        var pagination = await listingQueryPipeline.ValidateAndNormalizeAsync(query.Page, query.PageSize, cancellationToken);
        var scope = await ResolveScopeAsync(resolvedActor, query, cancellationToken);

        var totalItems = await listingRepository.CountUsersAsync(
            scope.CompanyId,
            query.Search,
            scope.RoleCodeFilter,
            query.IsActive,
            scope.RestrictedRoleCodes,
            cancellationToken);

        var rows = await listingRepository.GetUsersAsync(
            scope.CompanyId,
            query.Search,
            scope.RoleCodeFilter,
            query.IsActive,
            scope.RestrictedRoleCodes,
            pagination.Page,
            pagination.PageSize,
            cancellationToken);

        return new GetScopedUsersResponse
        {
            Items = rows.Select(row => new ScopedUserListItem
            {
                UserId = row.UserId,
                CompanyId = row.CompanyId,
                Email = row.Email,
                Username = row.Username,
                RoleCode = row.RoleCode,
                RoleName = row.RoleName,
                AccountEnabled = row.AccountEnabled,
                FirstName = row.FirstName,
                MiddleName = row.MiddleName,
                LastName = row.LastName,
                SecondLastName = row.SecondLastName,
                DocumentNumber = row.DocumentNumber,
                LinkedAtUtc = row.LinkedAtUtc,
                MembershipUpdatedAtUtc = row.MembershipUpdatedAtUtc
            }).ToArray(),
            Page = pagination.Page,
            PageSize = pagination.PageSize,
            TotalItems = totalItems
        };
    }

    private async Task<UserScopeDefinition> ResolveScopeAsync(
        ListingActorSnapshot actor,
        GetScopedUsersQuery query,
        CancellationToken cancellationToken)
    {
        var roleCodeFilter = NormalizeRoleCode(query.RoleCode);

        return query.Scope switch
        {
            ScopedUsersListingScope.Global => ResolveGlobalScope(actor, roleCodeFilter),
            ScopedUsersListingScope.CompanyById => await ResolveCompanyByIdScopeAsync(actor, query.CompanyId, roleCodeFilter, cancellationToken),
            ScopedUsersListingScope.MyCompany => ResolveMyCompanyScope(actor, roleCodeFilter),
            ScopedUsersListingScope.MyCompanyEstimatorsAuditors => ResolveEstimatorsAndAuditorsScope(actor, roleCodeFilter),
            _ => throw new ValidationAppException("Users listing scope is invalid.", "VALIDATION_ERROR")
        };
    }

    private static UserScopeDefinition ResolveGlobalScope(ListingActorSnapshot actor, string? roleCodeFilter)
    {
        if (actor.RoleCode != AdministratorRoleCode)
        {
            throw new ForbiddenAppException("Only Administrador can read global users list.", "USERS_READ_FORBIDDEN");
        }

        return new UserScopeDefinition(null, roleCodeFilter, null);
    }

    private async Task<UserScopeDefinition> ResolveCompanyByIdScopeAsync(
        ListingActorSnapshot actor,
        Guid? companyId,
        string? roleCodeFilter,
        CancellationToken cancellationToken)
    {
        if (!companyId.HasValue)
        {
            throw new ValidationAppException("companyId is required for this listing scope.", "VALIDATION_ERROR");
        }

        var companyExists = await listingRepository.CompanyExistsAsync(companyId.Value, cancellationToken);
        if (!companyExists)
        {
            throw new ConflictAppException("Company was not found.", "COMPANY_NOT_FOUND");
        }

        if (actor.RoleCode == AdministratorRoleCode)
        {
            return new UserScopeDefinition(companyId.Value, roleCodeFilter, null);
        }

        if (actor.RoleCode == DirectorRoleCode && actor.CompanyId == companyId.Value)
        {
            return new UserScopeDefinition(companyId.Value, roleCodeFilter, null);
        }

        throw new ForbiddenAppException("Actor cannot read users from this company.", "USERS_SCOPE_FORBIDDEN");
    }

    private static UserScopeDefinition ResolveMyCompanyScope(ListingActorSnapshot actor, string? roleCodeFilter)
    {
        if (actor.RoleCode is not DirectorRoleCode and not EstimatorRoleCode)
        {
            throw new ForbiddenAppException("Actor cannot read own company users list.", "USERS_SCOPE_FORBIDDEN");
        }

        if (!actor.CompanyId.HasValue)
        {
            throw new ForbiddenAppException("Company context is required for this role.", "COMPANY_CONTEXT_REQUIRED");
        }

        return new UserScopeDefinition(actor.CompanyId.Value, roleCodeFilter, null);
    }

    private static UserScopeDefinition ResolveEstimatorsAndAuditorsScope(ListingActorSnapshot actor, string? roleCodeFilter)
    {
        if (actor.RoleCode is not EstimatorRoleCode and not AuditorRoleCode)
        {
            throw new ForbiddenAppException("Actor cannot read estimators and auditors list.", "USERS_SCOPE_FORBIDDEN");
        }

        if (!actor.CompanyId.HasValue)
        {
            throw new ForbiddenAppException("Company context is required for this role.", "COMPANY_CONTEXT_REQUIRED");
        }

        return new UserScopeDefinition(
            actor.CompanyId.Value,
            roleCodeFilter,
            [EstimatorRoleCode, AuditorRoleCode]);
    }

    private static ListingActorSnapshot EnsureActorOperational(ListingActorSnapshot? actor)
    {
        var resolvedActor = actor ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!resolvedActor.AccountEnabled || resolvedActor.AccountLocked || !resolvedActor.RoleIsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        return resolvedActor;
    }

    private static string? NormalizeRoleCode(string? roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return null;
        }

        return roleCode.Trim().ToUpperInvariant();
    }

    private sealed record UserScopeDefinition(
        Guid? CompanyId,
        string? RoleCodeFilter,
        IReadOnlyCollection<string>? RestrictedRoleCodes);
}
