using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Users;

public sealed class UserSearchFiltersTests
{
    [Fact]
    public async Task GetUsers_Filters_By_Email_Username_Name_And_Document()
    {
        using var factory = new TestWebApplicationFactory();

        var adminActor = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");

        var unique = Guid.NewGuid().ToString("N")[..8];
        var target = await SeedSearchUserAsync(
            factory,
            roleCode: "PRESUPUESTADOR",
            companyId: null,
            firstName: $"Camila{unique}",
            lastName: $"Torres{unique}",
            email: $"search.{unique}@mail.local",
            username: $"search_{unique}",
            documentNumber: $"DOC-{unique}");

        await SeedSearchUserAsync(
            factory,
            roleCode: "PRESUPUESTADOR",
            companyId: null,
            firstName: "Another",
            lastName: "User",
            email: $"other.{unique}@mail.local",
            username: $"other_{unique}",
            documentNumber: $"OTHER-{unique}");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(adminActor.UserId, "ADMINISTRADOR")
            .Build();

        await AssertSingleMatchAsync(client, $"/api/v1/users?email={Uri.EscapeDataString(target.Email)}", target.UserId);
        await AssertSingleMatchAsync(client, $"/api/v1/users?username={Uri.EscapeDataString(target.Username)}", target.UserId);
        await AssertSingleMatchAsync(client, $"/api/v1/users?nombre_apellidos={Uri.EscapeDataString(target.FirstName)}", target.UserId);
        await AssertSingleMatchAsync(client, $"/api/v1/users?numero_documento={Uri.EscapeDataString(target.DocumentNumber)}", target.UserId);
    }

    private static async Task AssertSingleMatchAsync(HttpClient client, string path, Guid expectedUserId)
    {
        var response = await client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.ValueKind.Should().Be(JsonValueKind.Array);
        payload.GetArrayLength().Should().Be(1);
        payload[0].GetProperty("user_id").GetGuid().Should().Be(expectedUserId);
    }

    private static async Task<SeededSearchUser> SeedSearchUserAsync(
        TestWebApplicationFactory factory,
        string roleCode,
        Guid? companyId,
        string firstName,
        string lastName,
        string email,
        string username,
        string documentNumber)
    {
        var seededUser = await TestDataSeeder.SeedUserAsync(
            factory,
            roleCode,
            "Password1!",
            companyId: companyId,
            email: email,
            username: username,
            documentNumber: documentNumber);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var account = await dbContext.UserAccounts
            .Include(user => user.Profile)
            .SingleAsync(user => user.Id == seededUser.UserId);

        account.Profile!.FirstName = firstName;
        account.Profile.LastName = lastName;

        await dbContext.SaveChangesAsync();

        return new SeededSearchUser(
            account.Id,
            account.Email,
            account.Username,
            account.Profile.FirstName,
            account.Profile.DocumentNumber);
    }

    private sealed record SeededSearchUser(
        Guid UserId,
        string Email,
        string Username,
        string FirstName,
        string DocumentNumber);
}
