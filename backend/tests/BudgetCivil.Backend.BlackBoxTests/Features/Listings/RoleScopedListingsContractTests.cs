using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Listings;

public sealed class RoleScopedListingsContractTests
{
    [Fact]
    public async Task CompaniesUsers_Listing_Director_Cannot_Read_Another_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var companyA = await SeedCompanyAsync(factory, "Listado A");
        var companyB = await SeedCompanyAsync(factory, "Listado B");

        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyA);

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyA)
            .Build();

        var response = await directorClient.GetAsync($"/api/v1/companies/{companyB}/users?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompaniesUsers_Listing_Admin_Can_Read_Specific_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var companyA = await SeedCompanyAsync(factory, "Listado A");
        var companyB = await SeedCompanyAsync(factory, "Listado B");

        var admin = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");
        var userA = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!", companyId: companyA);
        _ = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!", companyId: companyB);

        var adminClient = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var response = await adminClient.GetAsync($"/api/v1/companies/{companyA}/users?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = payload.GetProperty("items").EnumerateArray().ToList();

        items.Should().NotBeEmpty();
        items.All(item => item.GetProperty("company_id").GetGuid() == companyA).Should().BeTrue();
        items.Any(item => item.GetProperty("user_id").GetGuid() == userA.UserId).Should().BeTrue();
    }

    [Fact]
    public async Task MeEstimatorsAuditors_Listing_Returns_Only_Estimator_And_Auditor_Roles()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Listado Rol");

        var estimadorActor = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Presupuestador, "Password1!", companyId: companyId);
        _ = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);
        _ = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Presupuestador, "Password1!", companyId: companyId);
        _ = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!", companyId: companyId);

        var estimadorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(estimadorActor.UserId, Feature002TestData.Roles.Presupuestador, companyId)
            .Build();

        var response = await estimadorClient.GetAsync("/api/v1/users/company/me/estimators-auditors?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var rows = payload.GetProperty("items").EnumerateArray().ToList();

        rows.Should().NotBeEmpty();
        rows.All(item =>
        {
            var role = item.GetProperty("role_code").GetString();
            return role is Feature002TestData.Roles.Presupuestador or Feature002TestData.Roles.Auditor;
        }).Should().BeTrue();
    }

    [Fact]
    public async Task Projects_Listing_Is_Scoped_By_Role()
    {
        using var factory = new TestWebApplicationFactory();

        var companyA = await SeedCompanyAsync(factory, "Proyecto A");
        var companyB = await SeedCompanyAsync(factory, "Proyecto B");

        var admin = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");
        var directorA = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyA);

        await SeedProjectAsync(factory, companyA, directorA.UserId, "Proyecto Alpha");
        await SeedProjectAsync(factory, companyB, admin.UserId, "Proyecto Beta");

        var adminClient = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var adminResponse = await adminClient.GetAsync("/api/v1/projects?pageNumber=1&pageSize=10");
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var adminPayload = await adminResponse.Content.ReadFromJsonAsync<JsonElement>();
        adminPayload.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(2);

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(directorA.UserId, Feature002TestData.Roles.DirectorProyectos, companyA)
            .Build();

        var directorResponse = await directorClient.GetAsync("/api/v1/projects?pageNumber=1&pageSize=10");
        directorResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var directorPayload = await directorResponse.Content.ReadFromJsonAsync<JsonElement>();
        var directorRows = directorPayload.GetProperty("items").EnumerateArray().ToList();

        directorRows.Should().NotBeEmpty();
        directorRows.All(row => row.GetProperty("company_id").GetGuid() == companyA).Should().BeTrue();
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
            TaxId = $"LS{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }

    private static async Task SeedProjectAsync(TestWebApplicationFactory factory, Guid companyId, Guid ownerUserId, string name)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        dbContext.Projects.Add(new Project
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            Description = "Proyecto para listados",
            Objective = "Validar alcance",
            Status = ProjectStatus.PorCotizar,
            IsPublic = false,
            SigningMode = Project.UniqueSignerByAssignedEstimator,
            DirectorOwnerUserId = ownerUserId,
            CreatedByUserId = ownerUserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }
}
