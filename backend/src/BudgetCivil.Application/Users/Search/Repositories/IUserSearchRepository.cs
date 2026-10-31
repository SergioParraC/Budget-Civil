namespace BudgetCivil.Application.Users.Search.Repositories;

public interface IUserSearchRepository
{
    Task<IReadOnlyList<UserSearchResult>> SearchAsync(
        Guid actorUserId,
        UserSearchQuery query,
        CancellationToken cancellationToken = default);

    Task<UserSearchResult?> GetByIdAsync(
        Guid actorUserId,
        Guid targetUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSearchResult>> SearchRepresentativeCandidatesAsync(
        Guid actorUserId,
        UserSearchQuery query,
        CancellationToken cancellationToken = default);
}
