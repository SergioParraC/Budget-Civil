using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Search;

public sealed class UserSearchResult
{
    public Guid UserId { get; init; }

    public Guid? CompanyId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string RoleCode { get; init; } = string.Empty;

    public bool AccountEnabled { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string? MiddleName { get; init; }

    public string LastName { get; init; } = string.Empty;

    public string? SecondLastName { get; init; }

    public string DocumentNumber { get; init; } = string.Empty;
}
