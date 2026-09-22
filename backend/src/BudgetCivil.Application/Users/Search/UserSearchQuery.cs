namespace BudgetCivil.Application.Users.Search;

public sealed class UserSearchQuery
{
    public string? Email { get; init; }

    public string? Username { get; init; }

    public string? FullName { get; init; }

    public string? DocumentNumber { get; init; }

    public bool? AccountEnabled { get; init; }
}
