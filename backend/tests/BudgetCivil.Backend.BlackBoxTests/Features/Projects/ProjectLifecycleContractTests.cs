using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Projects;

public sealed class ProjectLifecycleContractTests
{
    [Fact]
    public async Task ProjectLifecycle_Create_Update_And_Transitions_Work_With_Valid_Flow()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Project Lifecycle");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);

        await EnableSignatureAsync(factory, director.UserId);

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var createPayload = new
        {
            name = $"Proyecto {Feature002TestData.UniqueSuffix()}",
            description = "Proyecto de contrato",
            objective = "Validar ciclo de vida",
            signing_mode = "UNICA_POR_PRESUPUESTADOR_ASIGNADO"
        };

        var createResponse = await directorClient.PostAsJsonAsync("/api/v1/projects", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var projectId = createBody.GetProperty("project_id").GetGuid();
        createBody.GetProperty("status").GetString().Should().Be("POR_COTIZAR");

        var updateResponse = await directorClient.PutAsJsonAsync(
            $"/api/v1/projects/{projectId}",
            new
            {
                name = $"Proyecto Actualizado {Feature002TestData.UniqueSuffix()}",
                description = "Descripcion actualizada"
            });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var transitionToFalta = await directorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status",
            new { target_status = "FALTA_INFORMACION", reason = "Inicio de flujo" });

        transitionToFalta.StatusCode.Should().Be(HttpStatusCode.OK);

        var transitionToCotizado = await directorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status",
            new { target_status = "COTIZADO", reason = "Cotizado" });

        transitionToCotizado.StatusCode.Should().Be(HttpStatusCode.OK);

        var transitionToFinalizado = await directorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status",
            new { target_status = "FINALIZADO", reason = "Aprobado" });

        transitionToFinalizado.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await directorClient.GetAsync($"/api/v1/projects/{projectId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        getBody.GetProperty("status").GetString().Should().Be("FINALIZADO");
    }

    [Fact]
    public async Task ProjectStatus_Rejects_Invalid_Transition()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Invalid Transition");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var createResponse = await directorClient.PostAsJsonAsync(
            "/api/v1/projects",
            new
            {
                name = $"Proyecto {Feature002TestData.UniqueSuffix()}",
                signing_mode = "UNICA_POR_PRESUPUESTADOR_ASIGNADO"
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var projectId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("project_id").GetGuid();

        var invalidTransition = await directorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/status",
            new { target_status = "FINALIZADO", reason = "Salto invalido" });

        invalidTransition.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await invalidTransition.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("code").GetString().Should().Be("PROJECT_STATE_TRANSITION_INVALID");
    }

    [Fact]
    public async Task ProjectVisibility_Change_Is_Persisted()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Visibility");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);

        var directorClient = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var createResponse = await directorClient.PostAsJsonAsync(
            "/api/v1/projects",
            new
            {
                name = $"Proyecto {Feature002TestData.UniqueSuffix()}",
                signing_mode = "UNICA_POR_PRESUPUESTADOR_ASIGNADO"
            });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var projectId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("project_id").GetGuid();

        var visibilityResponse = await directorClient.PatchAsJsonAsync(
            $"/api/v1/projects/{projectId}/visibility",
            new { is_public = true, reason = "Publicar" });

        visibilityResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await visibilityResponse.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("new_is_public").GetBoolean().Should().BeTrue();
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
            TaxId = $"PJ{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }

    private static async Task EnableSignatureAsync(TestWebApplicationFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var profile = await dbContext.UserProfiles.SingleAsync(existingProfile => existingProfile.UserId == userId);
        profile.ProfessionalCardFilePath = "signatures/director.png";

        await dbContext.SaveChangesAsync();
    }
}
