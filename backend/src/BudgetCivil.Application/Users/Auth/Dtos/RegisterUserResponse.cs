using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Auth.Dtos;

public sealed class RegisterUserResponse
{
    public Guid UserId { get; init; }

    public string AssignedRole { get; init; } = string.Empty;

    public Guid? CompanyId { get; init; }

    public bool AccountEnabled { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
