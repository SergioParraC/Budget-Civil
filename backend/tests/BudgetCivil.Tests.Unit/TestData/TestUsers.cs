using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Tests.Unit.TestData;

internal static class TestUsers
{
    public const string Administrator = "ADMINISTRADOR";
    public const string Director = "DIRECTOR";
    public const string Estimator = "PRESUPUESTADOR";
    public const string Auditor = "AUDITOR";

    public static UserAccount WithRole(
        string roleCode,
        Guid? companyId = null,
        bool accountEnabled = true,
        bool accountLocked = false,
        bool roleIsActive = true)
    {
        var roleId = Guid.NewGuid();

        return new UserAccount
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            RoleId = roleId,
            AccountEnabled = accountEnabled,
            AccountLocked = accountLocked,
            Username = $"user_{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            FirstName = "Test",
            LastName = "User",
            DocumentType = "CC",
            DocumentNumber = Guid.NewGuid().ToString("N"),
            BirthDate = new DateOnly(1990, 1, 1),
            Role = new Role { Id = roleId, Code = roleCode, Name = roleCode, IsActive = roleIsActive }
        };
    }
}
