using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Companies;

public sealed class CompanyInactiveAccessTests
{
    [Fact]
    public async Task CompanyInactive_UserScopeRequest_Is_Forbidden_With_Functional_Code()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Inactive Access");
        var managerUser = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);

        await DeactivateCompanyAsync(factory, companyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(managerUser.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var response = await client.GetAsync("/api/v1/profile/me");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("code").GetString().Should().Be("COMPANY_INACTIVE");
    }

    private static async Task<Guid> SeedCompanyAsync(TestWebApplicationFactory factory, string prefix)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var companyId = Guid.NewGuid();
        var token = companyId.ToString("N")[..10].ToUpperInvariant();

        dbContext.Companies.Add(new Company
        {
            Id = companyId,
            BusinessName = $"{prefix} {token}",
            TaxId = $"TI{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }

    private static async Task DeactivateCompanyAsync(TestWebApplicationFactory factory, Guid companyId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var company = await dbContext.Companies.SingleAsync(existingCompany => existingCompany.Id == companyId);
        company.IsActive = false;
        company.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }
}
