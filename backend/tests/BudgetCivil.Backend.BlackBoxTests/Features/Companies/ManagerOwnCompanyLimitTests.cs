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

public sealed class ManagerOwnCompanyLimitTests
{
    [Fact]
    public async Task CreateCompany_ManagerWithoutCompany_Creates_Own_Company_And_Assigns_Context()
    {
        using var factory = new TestWebApplicationFactory();

        var managerUser = await TestDataSeeder.SeedUserAsync(factory, "GERENTE_PROYECTOS", "Password1!", companyId: null);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(managerUser.UserId, "GERENTE_PROYECTOS")
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var payload = new
        {
            business_name = $"Compania Gerente {unique}",
            tax_id = $"TM{unique}"[..12],
            is_active = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/companies", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responsePayload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var createdCompanyId = responsePayload.GetProperty("company_id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var refreshedManager = await dbContext.UserAccounts.SingleAsync(user => user.Id == managerUser.UserId);
        refreshedManager.CompanyId.Should().Be(createdCompanyId);

        var activeMembership = await dbContext.UserCompanyMemberships.SingleAsync(membership =>
            membership.UserId == managerUser.UserId && membership.IsActive);

        activeMembership.CompanyId.Should().Be(createdCompanyId);
        activeMembership.AssignedByUserId.Should().Be(managerUser.UserId);
    }

    [Fact]
    public async Task CreateCompany_ManagerWithExistingCompany_Is_Rejected_For_Second_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var existingCompanyId = await SeedCompanyAsync(factory, "Existing Manager Company");
        var managerUser = await TestDataSeeder.SeedUserAsync(factory, "GERENTE_PROYECTOS", "Password1!", companyId: existingCompanyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(managerUser.UserId, "GERENTE_PROYECTOS", existingCompanyId)
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var payload = new
        {
            business_name = $"Segunda Compania {unique}",
            tax_id = $"TN{unique}"[..12],
            is_active = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/companies", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateCompany_Manager_Cannot_Edit_Another_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var ownCompanyId = await SeedCompanyAsync(factory, "Own Manager Company");
        var otherCompanyId = await SeedCompanyAsync(factory, "Other Company");
        var managerUser = await TestDataSeeder.SeedUserAsync(factory, "GERENTE_PROYECTOS", "Password1!", companyId: ownCompanyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(managerUser.UserId, "GERENTE_PROYECTOS", ownCompanyId)
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var payload = new
        {
            business_name = $"Edited By Manager {unique}",
            tax_id = $"TO{unique}"[..12],
            is_active = true
        };

        var response = await client.PutAsJsonAsync($"/api/v1/companies/{otherCompanyId}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<Guid> SeedCompanyAsync(TestWebApplicationFactory factory, string namePrefix)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var companyId = Guid.NewGuid();
        var token = companyId.ToString("N")[..10].ToUpperInvariant();

        dbContext.Companies.Add(new Company
        {
            Id = companyId,
            BusinessName = $"{namePrefix} {token}",
            TaxId = $"TP{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
