using BudgetCivil.Domain.Projects;

namespace BudgetCivil.Application.Listings.Repositories;

public sealed record ListingActorSnapshot(
    Guid UserId,
    Guid? CompanyId,
    string RoleCode,
    bool AccountEnabled,
    bool AccountLocked,
    bool RoleIsActive);

public sealed record ListingUserRow(
    Guid UserId,
    Guid? CompanyId,
    string Email,
    string Username,
    string RoleCode,
    bool AccountEnabled,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? SecondLastName,
    string DocumentNumber);

public sealed record ListingProjectRow(
    Guid ProjectId,
    Guid CompanyId,
    string Name,
    ProjectStatus Status,
    bool IsPublic);

public interface IListingRepository
{
    Task<ListingActorSnapshot?> GetActorSnapshotAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    Task<bool> CompanyExistsAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<int> CountUsersAsync(
        Guid? companyId,
        string? search,
        string? roleCode,
        bool? isActive,
        IReadOnlyCollection<string>? restrictedRoleCodes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListingUserRow>> GetUsersAsync(
        Guid? companyId,
        string? search,
        string? roleCode,
        bool? isActive,
        IReadOnlyCollection<string>? restrictedRoleCodes,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountProjectsAsync(
        Guid? companyId,
        ProjectStatus? status,
        string? search,
        bool includeInhabilitado,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ListingProjectRow>> GetProjectsAsync(
        Guid? companyId,
        ProjectStatus? status,
        string? search,
        int pageNumber,
        int pageSize,
        bool includeInhabilitado,
        CancellationToken cancellationToken = default);
}
