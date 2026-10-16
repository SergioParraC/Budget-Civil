using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Projects;

public sealed class ProjectStatusAuthorizationTests
{
    [Fact]
    public async Task Presupuestador_Cannot_Disable_Project()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Status Auth");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);
        var estimador = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Presupuestador, "Password1!", companyId: companyId);

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var projectId = await CreateProjectAsync(directorClient);

        var estimadorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(estimador.UserId, Feature002TestData.Roles.Presupuestador, companyId)
            .Build();

        var response = await estimadorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status",
            new { target_status = "INHABILITADO", reason = "Sin permiso" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Auditor_Cannot_Change_Project_Status()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Status Auth");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);
        var auditor = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Auditor, "Password1!", companyId: companyId);

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var projectId = await CreateProjectAsync(directorClient);

        var auditorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(auditor.UserId, Feature002TestData.Roles.Auditor, companyId)
            .Build();

        var response = await auditorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status",
            new { target_status = "FALTA_INFORMACION", reason = "Sin permiso" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Director_Can_Disable_Project()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Status Auth");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var projectId = await CreateProjectAsync(directorClient);

        var response = await directorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status",
            new { target_status = "INHABILITADO", reason = "Cierre" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("new_status").GetString().Should().Be("INHABILITADO");
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
            TaxId = $"PS{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
