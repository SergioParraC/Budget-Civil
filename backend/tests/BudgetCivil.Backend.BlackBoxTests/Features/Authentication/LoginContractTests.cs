using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using FluentAssertions;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Authentication;

public sealed class LoginContractTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Login_Allows_Identifier_By_Email_Or_Username(bool useEmail)
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var seededUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!");
        var identifier = useEmail ? seededUser.Email : seededUser.Username;

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier,
            password = seededUser.Password
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("access_token").GetString().Should().NotBeNullOrWhiteSpace();
        payload.GetProperty("user").GetProperty("email").GetString().Should().Be(seededUser.Email);
        payload.GetProperty("user").GetProperty("username").GetString().Should().Be(seededUser.Username);
    }

    [Fact]
    public async Task Login_Returns_Unauthorized_For_Invalid_Credentials()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var seededUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!");

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = seededUser.Username,
            password = "WrongPassword1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_Returns_Locked_With_Required_Message_For_Disabled_Account()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var seededUser = await TestDataSeeder.SeedUserAsync(
            factory,
            "PRESUPUESTADOR",
            "Password1!",
            accountEnabled: false);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = seededUser.Email,
            password = seededUser.Password
        });

        response.StatusCode.Should().Be((HttpStatusCode)423);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("detail").GetString().Should().Be("Este no se encuentra habilitado, solicite al administrador su habilitacion");
    }
}
