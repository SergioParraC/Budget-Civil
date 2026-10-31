using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Users.Search.Repositories;

namespace BudgetCivil.Application.Users.Search.UseCases;

public sealed class GetUserByIdUseCase(IUserSearchRepository repository)
{
    public async Task<UserSearchResult> ExecuteAsync(
        Guid actorUserId,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(actorUserId, targetUserId, cancellationToken);

        return user ?? throw new ConflictAppException("User was not found.", "USER_NOT_FOUND");
    }
}
