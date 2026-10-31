using System.Text.Json.Serialization;

namespace BudgetCivil.Application.Users.Administration.Dtos;

public sealed class CreateUserResponse
{
    public Guid UserId { get; init; }

    public string AssignedRole { get; init; } = string.Empty;

    public Guid? CompanyId { get; init; }

    public bool AccountEnabled { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class UpdateUserResponse
{
    public Guid UserId { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class DeactivateUserResponse
{
    public Guid UserId { get; init; }

    public bool AccountEnabled { get; init; }
}

public sealed class EnableUserResponse
{
    public Guid UserId { get; init; }

    public bool AccountEnabled { get; init; }
}

public sealed class ChangeUserRoleResponse
{
    public Guid UserId { get; init; }

    public string AssignedRole { get; init; } = string.Empty;
}

public sealed class ChangeUserCompanyResponse
{
    public Guid UserId { get; init; }

    public Guid CompanyId { get; init; }
}
