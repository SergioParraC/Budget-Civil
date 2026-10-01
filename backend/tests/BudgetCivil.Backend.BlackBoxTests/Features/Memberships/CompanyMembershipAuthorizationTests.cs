using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Memberships;

public sealed class CompanyMembershipAuthorizationTests
{
    [Fact]
    public async Task Presupuestador_Cannot_Assign_Users_To_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Membership Auth");
        var estimador = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Presupuestador, "Password1!", companyId: companyId);
        var targetAuditor = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!");

        var estimadorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(estimador.UserId, Feature002TestData.Roles.Presupuestador, companyId)
            .Build();

        var response = await estimadorClient.PostAsJsonAsync(
            $"/api/v1/companies/{companyId}/memberships",
            new
            {
                user_id = targetAuditor.UserId,
                role_code = Feature002TestData.Roles.Auditor
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Director_Can_Assign_Allowed_Roles_In_Own_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Membership Auth");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);
        var targetAuditor = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!");

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var response = await directorClient.PostAsJsonAsync(
            $"/api/v1/companies/{companyId}/memberships",
            new
            {
                user_id = targetAuditor.UserId,
                role_code = Feature002TestData.Roles.Auditor
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("is_active").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Director_Cannot_Assign_Users_To_Another_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var companyA = await SeedCompanyAsync(factory, "Membership A");
        var companyB = await SeedCompanyAsync(factory, "Membership B");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyA);
        var targetAuditor = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!");

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyA)
            .Build();

        var response = await directorClient.PostAsJsonAsync(
            $"/api/v1/companies/{companyB}/memberships",
            new
            {
                user_id = targetAuditor.UserId,
                role_code = Feature002TestData.Roles.Auditor
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Auditor_Cannot_Assign_Users_To_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Membership Auth");
        var auditor = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!", companyId: companyId);
        var targetEstimador = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Presupuestador, "Password1!");

        var auditorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(auditor.UserId, Feature002TestData.Roles.Auditor, companyId)
            .Build();

        var response = await auditorClient.PostAsJsonAsync(
            $"/api/v1/companies/{companyId}/memberships",
            new
            {
                user_id = targetEstimador.UserId,
                role_code = Feature002TestData.Roles.Presupuestador
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
            TaxId = $"CM{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
