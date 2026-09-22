using System.Net;
using System.Net.Http.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Profile;

public sealed class ChangePasswordPolicyTests
{
    [Fact]
    public async Task ChangeMyPassword_Rejects_NewPassword_Outside_Policy()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Password Policy Company");
        var user = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(user.UserId, "PRESUPUESTADOR", companyId)
            .Build();

        var response = await client.PostAsJsonAsync("/api/v1/profile/me/change-password", new
        {
            current_password = "Password1!",
            new_password = "weak"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeMyPassword_Rejects_Invalid_Current_Password()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Current Password Company");
        var user = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(user.UserId, "PRESUPUESTADOR", companyId)
            .Build();

        var response = await client.PostAsJsonAsync("/api/v1/profile/me/change-password", new
        {
            current_password = "Incorrect1!",
            new_password = "UpdatedPass1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangeMyPassword_Updates_Credentials_And_Allows_New_Login()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Password Update Company");
        var user = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyId);

        var profileClient = new AuthenticatedClientBuilder(factory)
            .WithUser(user.UserId, "PRESUPUESTADOR", companyId)
            .Build();

        var changeResponse = await profileClient.PostAsJsonAsync("/api/v1/profile/me/change-password", new
        {
            current_password = "Password1!",
            new_password = "UpdatedPass1!"
        });

        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginClient = factory.CreateClient();

        var oldPasswordLogin = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = user.Username,
            password = "Password1!"
        });

        oldPasswordLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newPasswordLogin = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = user.Username,
            password = "UpdatedPass1!"
        });

        newPasswordLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<Guid> SeedCompanyAsync(TestWebApplicationFactory factory, string prefix)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var companyId = Guid.NewGuid();
        var token = companyId.ToString("N")[..10].ToUpperInvariant();

        dbContext.Companies.Add(new BudgetCivil.Domain.Users.Entities.Company
        {
            Id = companyId,
            BusinessName = $"{prefix} {token}",
            TaxId = $"TP{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
