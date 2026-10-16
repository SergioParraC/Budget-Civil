using BudgetCivil.Application.Users.Search.Repositories;

namespace BudgetCivil.Application.Users.Search.UseCases;

public sealed class SearchUsersUseCase(IUserSearchRepository repository)
{
    public Task<IReadOnlyList<UserSearchResult>> ExecuteAsync(
        Guid actorUserId,
        UserSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        return repository.SearchAsync(actorUserId, query, cancellationToken);
    }
}
