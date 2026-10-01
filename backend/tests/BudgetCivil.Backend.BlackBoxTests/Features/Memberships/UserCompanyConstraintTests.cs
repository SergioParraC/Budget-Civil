using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Memberships;

public sealed class UserCompanyConstraintTests
{
    [Fact]
    public async Task NonAdmin_User_Cannot_Have_More_Than_One_Active_Company_Membership()
    {
        using var factory = new TestWebApplicationFactory();

        var companyA = await SeedCompanyAsync(factory, "Company A");
        var companyB = await SeedCompanyAsync(factory, "Company B");
        var admin = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");
        var targetUser = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Presupuestador, "Password1!");

        var adminClient = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var firstAssignment = await adminClient.PostAsJsonAsync(
            $"/api/v1/companies/{companyA}/memberships",
            new
            {
                user_id = targetUser.UserId,
                role_code = Feature002TestData.Roles.Presupuestador
            });

        firstAssignment.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondAssignment = await adminClient.PostAsJsonAsync(
            $"/api/v1/companies/{companyB}/memberships",
            new
            {
                user_id = targetUser.UserId,
                role_code = Feature002TestData.Roles.Presupuestador
            });

        secondAssignment.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var payload = await secondAssignment.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("code").GetString().Should().Be("COMPANY_MEMBERSHIP_LIMIT_REACHED");
    }

    [Fact]
    public async Task Administrador_Cannot_Be_Assigned_To_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Company A");
        var adminActor = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");
        var adminTarget = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");

        var adminClient = new AuthenticatedClientBuilder(factory)
            .WithUser(adminActor.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var response = await adminClient.PostAsJsonAsync(
            $"/api/v1/companies/{companyId}/memberships",
            new
            {
                user_id = adminTarget.UserId,
                role_code = Feature002TestData.Roles.DirectorProyectos
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("code").GetString().Should().Be("ADMIN_COMPANY_MEMBERSHIP_FORBIDDEN");
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
            TaxId = $"UC{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
