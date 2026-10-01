using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Memberships;

public sealed class ProjectMembershipAuthorizationTests
{
    [Fact]
    public async Task Director_Can_Assign_Affiliated_User_To_Project()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Project Membership");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);
        var targetAuditor = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!", companyId: companyId);

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var projectId = await CreateProjectAsync(directorClient);

        var response = await directorClient.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/memberships",
            new
            {
                user_id = targetAuditor.UserId,
                role_code = Feature002TestData.Roles.Auditor
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Director_Cannot_Assign_User_Not_Affiliated_To_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Project Membership");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);
        var targetAuditor = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!");

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var projectId = await CreateProjectAsync(directorClient);

        var response = await directorClient.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/memberships",
            new
            {
                user_id = targetAuditor.UserId,
                role_code = Feature002TestData.Roles.Auditor
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Presupuestador_Cannot_Assign_Director_To_Project()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Project Membership");
        var ownerDirector = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);
        var estimador = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Presupuestador, "Password1!", companyId: companyId);
        var targetDirector = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);

        var ownerClient = new AuthenticatedClientBuilder(factory)
            .WithUser(ownerDirector.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var estimadorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(estimador.UserId, Feature002TestData.Roles.Presupuestador, companyId)
            .Build();

        var projectId = await CreateProjectAsync(ownerClient);

        var response = await estimadorClient.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/memberships",
            new
            {
                user_id = targetDirector.UserId,
                role_code = Feature002TestData.Roles.DirectorProyectos
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Auditor_Cannot_Assign_Users_To_Project()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Project Membership");
        var ownerDirector = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);
        var auditor = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!", companyId: companyId);
        var targetEstimador = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Presupuestador, "Password1!", companyId: companyId);

        var ownerClient = new AuthenticatedClientBuilder(factory)
            .WithUser(ownerDirector.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var auditorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(auditor.UserId, Feature002TestData.Roles.Auditor, companyId)
            .Build();

        var projectId = await CreateProjectAsync(ownerClient);

        var response = await auditorClient.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/memberships",
            new
            {
                user_id = targetEstimador.UserId,
                role_code = Feature002TestData.Roles.Presupuestador
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Administrador_Can_Assign_Only_Director_To_Project()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Project Membership");
        var ownerDirector = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);
        var admin = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");
        var targetAuditor = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!", companyId: companyId);
        var targetDirector = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);

        var ownerClient = new AuthenticatedClientBuilder(factory)
            .WithUser(ownerDirector.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var adminClient = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var projectId = await CreateProjectAsync(ownerClient);

        var invalidRoleResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/memberships",
            new
            {
                user_id = targetAuditor.UserId,
                role_code = Feature002TestData.Roles.Auditor
            });

        invalidRoleResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var validRoleResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/projects/{projectId}/memberships",
            new
            {
                user_id = targetDirector.UserId,
                role_code = Feature002TestData.Roles.DirectorProyectos
            });

        validRoleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await validRoleResponse.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("project_id").GetGuid().Should().Be(projectId);
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client)
    {
        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/projects",
            new
            {
                name = $"Proyecto {Feature002TestData.UniqueSuffix()}",
                signing_mode = "UNICA_POR_PRESUPUESTADOR_ASIGNADO"
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("project_id").GetGuid();
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
            TaxId = $"PM{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
