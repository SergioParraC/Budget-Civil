using BudgetCivil.Application.Common.Security;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Infrastructure;

public sealed record SeededUser(Guid UserId, string Email, string Username, string Password);

public static class TestDataSeeder
{
    public static async Task<SeededUser> SeedUserAsync(
        TestWebApplicationFactory factory,
        string roleCode,
        string password,
        bool accountEnabled = true,
        bool accountLocked = false,
        Guid? companyId = null,
        string? email = null,
        string? username = null,
        string? documentNumber = null)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var role = await dbContext.Roles.SingleAsync(existingRole => existingRole.Code == roleCode);

        Company? company = null;
        if (companyId.HasValue)
        {
            company = await dbContext.Companies.SingleOrDefaultAsync(existingCompany => existingCompany.Id == companyId.Value);
            if (company is null)
            {
                company = new Company
                {
                    Id = companyId.Value,
                    BusinessName = $"Compania {companyId.Value:N}",
                    TaxId = $"TAX-{companyId.Value:N}".Substring(0, 20),
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                dbContext.Companies.Add(company);
            }
        }

        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var userId = Guid.NewGuid();

        var userAccount = new UserAccount
        {
            Id = userId,
            RoleId = role.Id,
            CompanyId = company?.Id,
            Email = email ?? $"user.{uniqueSuffix[..8]}@test.local",
            Username = username ?? $"user_{uniqueSuffix[..10]}",
            PasswordHash = passwordHasher.HashPassword(password),
            AccountEnabled = accountEnabled,
            AccountLocked = accountLocked,
            FailedLoginAttempts = 0,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        userAccount.Profile = new UserProfile
        {
            UserId = userId,
            FirstName = "Test",
            LastName = "User",
            DocumentType = "CC",
            DocumentNumber = documentNumber ?? $"DOC-{uniqueSuffix[..12]}",
            BirthDate = new DateOnly(2000, 1, 1),
            ResidenceCity = "Bogota",
            ResidenceDepartment = "Cundinamarca",
            ResidenceCountry = "Colombia"
        };

        dbContext.UserAccounts.Add(userAccount);
        await dbContext.SaveChangesAsync();

        return new SeededUser(userAccount.Id, userAccount.Email, userAccount.Username, password);
    }

    public static async Task<UserAccount?> GetUserAsync(TestWebApplicationFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        return await dbContext.UserAccounts
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Id == userId);
    }
}
