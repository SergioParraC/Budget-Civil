using BudgetCivil.Application.Common.Security;
using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Infrastructure;

public sealed record SeededUser(Guid UserId, string Email, string Username, string Password);
public sealed record SeededLocation(Guid CountryId, Guid DepartmentId, Guid MunicipalityId);

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
        var location = await SeedLocationAsync(dbContext);

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
                    CreatedAtUtc = DateTime.UtcNow
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
            CountryId = location.CountryId,
            DepartmentId = location.DepartmentId,
            MunicipalityId = location.MunicipalityId
        };

        dbContext.UserAccounts.Add(userAccount);
        await dbContext.SaveChangesAsync();

        return new SeededUser(userAccount.Id, userAccount.Email, userAccount.Username, password);
    }

    public static async Task<SeededLocation> SeedLocationAsync(TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();
        var location = await SeedLocationAsync(dbContext);
        await dbContext.SaveChangesAsync();
        return location;
    }

    private static async Task<SeededLocation> SeedLocationAsync(BudgetCivilDbContext dbContext)
    {
        const string countryCode = "TEST";
        const string departmentCode = "TST-DEP";
        const string municipalityCode = "TST-MUN";

        var country = await dbContext.Countries.SingleOrDefaultAsync(item => item.Code == countryCode);
        if (country is null)
        {
            country = new Country { Id = Guid.NewGuid(), Code = countryCode, Name = "Test Country", IsActive = true };
            dbContext.Countries.Add(country);
        }

        var department = await dbContext.Departments.SingleOrDefaultAsync(item =>
            item.CountryId == country.Id && item.Code == departmentCode);
        if (department is null)
        {
            department = new Department
            {
                Id = Guid.NewGuid(),
                CountryId = country.Id,
                Code = departmentCode,
                Name = "Test Department",
                IsActive = true
            };
            dbContext.Departments.Add(department);
        }

        var municipality = await dbContext.Municipalities.SingleOrDefaultAsync(item =>
            item.DepartmentId == department.Id && item.Code == municipalityCode);
        if (municipality is null)
        {
            municipality = new Municipality
            {
                Id = Guid.NewGuid(),
                DepartmentId = department.Id,
                Code = municipalityCode,
                Name = "Test Municipality",
                IsActive = true
            };
            dbContext.Municipalities.Add(municipality);
        }

        return new SeededLocation(country.Id, department.Id, municipality.Id);
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
