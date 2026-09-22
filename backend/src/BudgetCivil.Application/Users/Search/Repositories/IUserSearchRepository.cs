namespace BudgetCivil.Application.Users.Search.Repositories;

public interface IUserSearchRepository
{
    Task<IReadOnlyList<UserSearchResult>> SearchAsync(
        Guid actorUserId,
        UserSearchQuery query,
        CancellationToken cancellationToken = default);
}
