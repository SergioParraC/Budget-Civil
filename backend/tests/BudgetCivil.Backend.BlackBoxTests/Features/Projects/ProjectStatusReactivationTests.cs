using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Projects;

public sealed class ProjectStatusReactivationTests
{
    [Fact]
    public async Task Finalize_Requires_Active_Signature()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Signature");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var projectId = await CreateProjectAsync(client);

        (await client.PatchAsJsonAsync($"/api/v1/projects/{projectId}/status", new { target_status = "FALTA_INFORMACION" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.PatchAsJsonAsync($"/api/v1/projects/{projectId}/status", new { target_status = "COTIZADO" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var finalizeResponse = await client.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status",
            new { target_status = "FINALIZADO" });

        finalizeResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var payload = await finalizeResponse.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("code").GetString().Should().Be("PROJECT_STATE_PRECONDITION_FAILED");
    }

    [Fact]
    public async Task Reactivate_From_Inhabilitado_Allows_Director_And_Administrador()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Reactivate");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);
        var admin = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var adminClient = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var projectId = await CreateProjectAsync(directorClient);

        (await directorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status",
            new { target_status = "INHABILITADO", reason = "Pausado" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var directorReactivate = await directorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status/reactivate",
            new { reason = "Volver" });

        directorReactivate.StatusCode.Should().Be(HttpStatusCode.OK);

        (await directorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status",
            new { target_status = "INHABILITADO", reason = "Pausado2" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var adminReactivate = await adminClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status/reactivate",
            new { reason = "Recuperar" });

        adminReactivate.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await adminReactivate.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("new_status").GetString().Should().Be("FALTA_INFORMACION");
    }

    [Fact]
    public async Task Inhabilitado_Project_Is_Not_Visible_For_Presupuestador()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Visibility");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);
        var estimador = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Presupuestador, "Password1!", companyId: companyId);

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var estimadorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(estimador.UserId, Feature002TestData.Roles.Presupuestador, companyId)
            .Build();

        var projectId = await CreateProjectAsync(directorClient);

        (await directorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status",
            new { target_status = "INHABILITADO" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await estimadorClient.GetAsync($"/api/v1/projects/{projectId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
            TaxId = $"PR{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
