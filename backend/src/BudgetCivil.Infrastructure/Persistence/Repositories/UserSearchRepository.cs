using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Users.Search;
using BudgetCivil.Application.Users.Search.Repositories;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class UserSearchRepository(BudgetCivilDbContext dbContext) : IUserSearchRepository
{
    private const string AdministratorRoleCode = "ADMINISTRADOR";
    private const string DirectorRoleCode = "DIRECTOR";
    private const string EstimatorRoleCode = "PRESUPUESTADOR";
    private const string AuditorRoleCode = "AUDITOR";
    private const int RepresentativeCandidateLimit = 20;

    public async Task<IReadOnlyList<UserSearchResult>> SearchAsync(
        Guid actorUserId,
        UserSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var actorSnapshot = await GetActorSnapshotAsync(actorUserId, cancellationToken);
        var roleCodeFilter = NormalizeRoleCode(query.RoleCode);
        EnsureRoleFilterAllowed(actorSnapshot, roleCodeFilter);

        var searchQuery = dbContext.UserAccounts
            .AsNoTracking()
            .AsQueryable();

        if (query.UnassignedOnly)
        {
            // "Disponibles para vincular": solo Administrador y Director de proyectos.
            if (actorSnapshot.RoleCode is not AdministratorRoleCode and not DirectorRoleCode)
            {
                throw new ForbiddenAppException(
                    "Actor cannot search unassigned users.",
                    "USERS_SEARCH_UNASSIGNED_FORBIDDEN");
            }

            searchQuery = searchQuery.Where(user => !user.Memberships.Any(membership => membership.AccountEnabled));
        }
        else if (actorSnapshot.RoleCode != AdministratorRoleCode)
        {
            if (!actorSnapshot.CompanyId.HasValue)
            {
                throw new ForbiddenAppException("Company context is required for this role.", "COMPANY_CONTEXT_REQUIRED");
            }

            var actorCompanyId = actorSnapshot.CompanyId.Value;
            searchQuery = searchQuery.Where(user =>
                user.Memberships.Any(membership => membership.AccountEnabled && membership.CompanyId == actorCompanyId));
        }

        if (query.CompanyId.HasValue)
        {
            if (actorSnapshot.RoleCode != AdministratorRoleCode &&
                actorSnapshot.CompanyId != query.CompanyId.Value)
            {
                throw new ForbiddenAppException(
                    "Actor cannot search users from another company.",
                    "USERS_SEARCH_COMPANY_FORBIDDEN");
            }

            var requestedCompanyId = query.CompanyId.Value;
            searchQuery = searchQuery.Where(user =>
                user.Memberships.Any(membership => membership.AccountEnabled && membership.CompanyId == requestedCompanyId));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var normalizedSearch = query.Search.Trim().ToUpperInvariant();

            searchQuery = searchQuery.Where(user =>
                user.Email.ToUpper().Contains(normalizedSearch) ||
                user.Username.ToUpper().Contains(normalizedSearch) ||
                ((user.FirstName + " " +
                  (user.MiddleName ?? string.Empty) + " " +
                  user.LastName + " " +
                  (user.SecondLastName ?? string.Empty))
                .ToUpper())
                .Contains(normalizedSearch) ||
                user.DocumentNumber.ToUpper().Contains(normalizedSearch));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var normalizedEmail = query.Email.Trim().ToUpperInvariant();
            searchQuery = searchQuery.Where(user => user.Email.ToUpper().Contains(normalizedEmail));
        }

        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            var normalizedUsername = query.Username.Trim().ToUpperInvariant();
            searchQuery = searchQuery.Where(user => user.Username.ToUpper().Contains(normalizedUsername));
        }

        if (!string.IsNullOrWhiteSpace(query.FullName))
        {
            var normalizedFullName = query.FullName.Trim().ToUpperInvariant();

            searchQuery = searchQuery.Where(user =>
                ((user.FirstName + " " +
                  (user.MiddleName ?? string.Empty) + " " +
                  user.LastName + " " +
                  (user.SecondLastName ?? string.Empty))
                .ToUpper())
                .Contains(normalizedFullName));
        }

        if (!string.IsNullOrWhiteSpace(query.DocumentType))
        {
            var normalizedDocumentType = query.DocumentType.Trim().ToUpperInvariant();
            searchQuery = searchQuery.Where(user => user.DocumentType.ToUpper() == normalizedDocumentType);
        }

        if (!string.IsNullOrWhiteSpace(query.DocumentNumber))
        {
            var normalizedDocumentNumber = query.DocumentNumber.Trim().ToUpperInvariant();
            searchQuery = searchQuery.Where(user => user.DocumentNumber.ToUpper().Contains(normalizedDocumentNumber));
        }

        if (roleCodeFilter is not null)
        {
            searchQuery = searchQuery.Where(user => user.Role.Code == roleCodeFilter);
        }

        if (query.AccountEnabled.HasValue)
        {
            searchQuery = searchQuery.Where(user => user.AccountEnabled == query.AccountEnabled.Value);
        }

        return await ProjectSearchResultsAsync(searchQuery, cancellationToken);
    }

    public async Task<UserSearchResult?> GetByIdAsync(
        Guid actorUserId,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        var actorSnapshot = await GetActorSnapshotAsync(actorUserId, cancellationToken);
        EnsureActorCanPickRepresentative(actorSnapshot);

        var target = await ProjectSearchResultsAsync(
            dbContext.UserAccounts.AsNoTracking().Where(user => user.Id == targetUserId),
            cancellationToken);

        var result = target.SingleOrDefault();
        if (result is null)
        {
            return null;
        }

        EnsureTargetInActorScope(actorSnapshot, result.CompanyId);

        return result;
    }

    /// <summary>
    /// Candidatos a representante legal: usuarios activos, con el tipo y el número de documento
    /// obligatorios. El Administrador ve todas las cuentas; el Director solo las de su compañía y las
    /// que todavía no tienen compañía.
    /// </summary>
    public async Task<IReadOnlyList<UserSearchResult>> SearchRepresentativeCandidatesAsync(
        Guid actorUserId,
        UserSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.DocumentType) || string.IsNullOrWhiteSpace(query.DocumentNumber))
        {
            throw new ValidationAppException(
                "documentType and documentNumber are required.",
                "VALIDATION_ERROR");
        }

        var actorSnapshot = await GetActorSnapshotAsync(actorUserId, cancellationToken);
        EnsureActorCanPickRepresentative(actorSnapshot);

        var normalizedDocumentType = query.DocumentType.Trim().ToUpperInvariant();
        var normalizedDocumentNumber = query.DocumentNumber.Trim().ToUpperInvariant();

        var candidatesQuery = dbContext.UserAccounts
            .AsNoTracking()
            .Where(user => user.AccountEnabled && !user.AccountLocked)
            .Where(user => user.DocumentType.ToUpper() == normalizedDocumentType)
            .Where(user => user.DocumentNumber.ToUpper().Contains(normalizedDocumentNumber))
            .Where(user => user.Role.Code != AdministratorRoleCode);

        if (actorSnapshot.RoleCode == DirectorRoleCode)
        {
            var actorCompanyId = actorSnapshot.CompanyId;
            candidatesQuery = candidatesQuery.Where(user =>
                !user.Memberships.Any(membership => membership.AccountEnabled) ||
                (actorCompanyId.HasValue &&
                 user.Memberships.Any(membership => membership.AccountEnabled && membership.CompanyId == actorCompanyId.Value)));
        }

        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            var normalizedUsername = query.Username.Trim().ToUpperInvariant();
            candidatesQuery = candidatesQuery.Where(user => user.Username.ToUpper().Contains(normalizedUsername));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var normalizedEmail = query.Email.Trim().ToUpperInvariant();
            candidatesQuery = candidatesQuery.Where(user => user.Email.ToUpper().Contains(normalizedEmail));
        }

        var orderedQuery = candidatesQuery
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ThenBy(user => user.Username)
            .Take(RepresentativeCandidateLimit);

        return await ProjectSearchResultsAsync(orderedQuery, cancellationToken);
    }

    private async Task<IReadOnlyList<UserSearchResult>> ProjectSearchResultsAsync(
        IQueryable<UserAccount> query,
        CancellationToken cancellationToken)
    {
        return await query
            .Select(user => new UserSearchResult
            {
                UserId = user.Id,
                CompanyId = user.Memberships
                    .Where(membership => membership.AccountEnabled)
                    .Select(membership => (Guid?)membership.CompanyId)
                    .FirstOrDefault(),
                Email = user.Email,
                Username = user.Username,
                RoleCode = user.Role.Code,
                RoleName = user.Role.Name,
                AccountEnabled = user.AccountEnabled,
                FirstName = user.FirstName,
                MiddleName = user.MiddleName,
                LastName = user.LastName,
                SecondLastName = user.SecondLastName,
                DocumentType = user.DocumentType,
                DocumentNumber = user.DocumentNumber,
                LinkedAtUtc = user.Memberships
                    .Where(membership => membership.AccountEnabled)
                    .Select(membership => (DateTime?)membership.CreatedAtUtc)
                    .FirstOrDefault(),
                MembershipUpdatedAtUtc = user.Memberships
                    .Where(membership => membership.AccountEnabled)
                    .Select(membership => (DateTime?)membership.UpdatedAtUtc)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<ActorSearchSnapshot> GetActorSnapshotAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var actorSnapshot = await dbContext.UserAccounts
            .AsNoTracking()
            .Where(user => user.Id == actorUserId)
            .Select(user => new ActorSearchSnapshot(
                user.Memberships
                    .Where(membership => membership.AccountEnabled)
                    .OrderByDescending(membership => membership.UpdatedAtUtc)
                    .Select(membership => (Guid?)membership.CompanyId)
                    .FirstOrDefault(),
                user.Role.Code))
            .SingleOrDefaultAsync(cancellationToken);

        if (actorSnapshot is null)
        {
            throw new ForbiddenAppException("Authenticated user does not exist.", "AUTH_USER_NOT_FOUND");
        }

        return actorSnapshot;
    }

    private static string? NormalizeRoleCode(string? roleCode)
    {
        return string.IsNullOrWhiteSpace(roleCode) ? null : roleCode.Trim().ToUpperInvariant();
    }

    private static void EnsureRoleFilterAllowed(ActorSearchSnapshot actorSnapshot, string? roleCodeFilter)
    {
        if (roleCodeFilter is null || actorSnapshot.RoleCode == AdministratorRoleCode)
        {
            return;
        }

        // Director y Presupuestador solo pueden buscar los roles que pueden asignar a un proyecto
        // (`D-85`, `D-112`); el Auditor no asigna y recibe rechazo.
        if ((actorSnapshot.RoleCode is DirectorRoleCode or EstimatorRoleCode) &&
            (roleCodeFilter is EstimatorRoleCode or AuditorRoleCode))
        {
            return;
        }

        throw new ForbiddenAppException(
            "Actor cannot filter users by this role.",
            "USERS_SEARCH_ROLE_FORBIDDEN");
    }

    private static void EnsureActorCanPickRepresentative(ActorSearchSnapshot actorSnapshot)
    {
        if (actorSnapshot.RoleCode is not AdministratorRoleCode and not DirectorRoleCode)
        {
            throw new ForbiddenAppException(
                "Only Administrador and Director de proyectos can select a legal representative.",
                "REPRESENTATIVE_SEARCH_FORBIDDEN");
        }
    }

    private static void EnsureTargetInActorScope(ActorSearchSnapshot actorSnapshot, Guid? targetCompanyId)
    {
        if (actorSnapshot.RoleCode == AdministratorRoleCode)
        {
            return;
        }

        if (targetCompanyId.HasValue && targetCompanyId != actorSnapshot.CompanyId)
        {
            throw new ForbiddenAppException(
                "Target user belongs to another company.",
                "REPRESENTATIVE_SEARCH_FORBIDDEN");
        }
    }

    private sealed record ActorSearchSnapshot(Guid? CompanyId, string RoleCode);
}
