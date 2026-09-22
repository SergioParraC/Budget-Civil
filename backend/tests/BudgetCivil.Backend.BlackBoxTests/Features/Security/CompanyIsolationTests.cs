using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Security;

public sealed class CompanyIsolationTests
{
    [Fact]
    public async Task GetUsers_For_Manager_Returns_Only_Users_Within_Actor_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var companyA = await SeedCompanyAsync(factory, "Isolation A");
        var companyB = await SeedCompanyAsync(factory, "Isolation B");

        var managerActor = await TestDataSeeder.SeedUserAsync(factory, "GERENTE_PROYECTOS", "Password1!", companyId: companyA);
        _ = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyA);
        var foreignUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyB);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(managerActor.UserId, "GERENTE_PROYECTOS", companyA)
            .Build();

        var response = await client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.ValueKind.Should().Be(JsonValueKind.Array);
        payload.GetArrayLength().Should().BeGreaterThan(0);

        foreach (var row in payload.EnumerateArray())
        {
            row.GetProperty("company_id").GetGuid().Should().Be(companyA);
        }

        var emails = payload
            .EnumerateArray()
            .Select(row => row.GetProperty("email").GetString())
            .Where(value => value is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        emails.Should().NotContain(foreignUser.Email);

        var outOfScopeFilterResponse = await client.GetAsync($"/api/v1/users?email={Uri.EscapeDataString(foreignUser.Email)}");
        outOfScopeFilterResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var filteredPayload = await outOfScopeFilterResponse.Content.ReadFromJsonAsync<JsonElement>();
        filteredPayload.ValueKind.Should().Be(JsonValueKind.Array);
        filteredPayload.GetArrayLength().Should().Be(0);
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
            TaxId = $"CI{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
