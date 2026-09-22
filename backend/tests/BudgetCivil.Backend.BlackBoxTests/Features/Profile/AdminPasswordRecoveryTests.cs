using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Profile;

public sealed class AdminPasswordRecoveryTests
{
    [Fact]
    public async Task EnablePasswordRecovery_Allows_Administrador_And_Creates_Grant()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Recovery Admin Company");
        var adminUser = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");
        var targetUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, "ADMINISTRADOR")
            .Build();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/users/{targetUser.UserId}/password-recovery/enable",
            new
            {
                reason = "Recuperacion por bloqueos consecutivos"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responsePayload = await response.Content.ReadFromJsonAsync<JsonElement>();
        responsePayload.GetProperty("user_id").GetGuid().Should().Be(targetUser.UserId);
        responsePayload.GetProperty("recovery_enabled").GetBoolean().Should().BeTrue();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var grant = await dbContext.PasswordRecoveryGrants.SingleAsync(existingGrant =>
            existingGrant.UserId == targetUser.UserId &&
            existingGrant.IsActive);

        grant.EnabledByUserId.Should().Be(adminUser.UserId);
        grant.Reason.Should().Be("Recuperacion por bloqueos consecutivos");
        grant.ConsumedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task EnablePasswordRecovery_Rejects_Non_Admin_Actor()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Recovery Denied Company");
        var managerUser = await TestDataSeeder.SeedUserAsync(factory, "GERENTE_PROYECTOS", "Password1!", companyId: companyId);
        var targetUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(managerUser.UserId, "GERENTE_PROYECTOS", companyId)
            .Build();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/users/{targetUser.UserId}/password-recovery/enable",
            new
            {
                reason = "Solicitud operacional"
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
            TaxId = $"TR{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
