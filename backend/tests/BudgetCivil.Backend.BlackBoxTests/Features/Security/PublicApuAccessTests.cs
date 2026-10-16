using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using FluentAssertions;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Security;

public sealed class PublicApuAccessTests
{
    [Fact]
    public async Task Self_Registered_User_Can_Read_Public_Apu_Data()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        var location = await TestDataSeeder.SeedLocationAsync(factory);

        var unique = Guid.NewGuid().ToString("N")[..8];
        var registerPayload = new Dictionary<string, object?>
        {
            ["primer_nombre"] = "Ana",
            ["primer_apellido"] = "Lopez",
            ["email"] = $"ana.{unique}@mail.local",
            ["username"] = $"ana_{unique}",
            ["password"] = "Password1!",
            ["tipo_documento"] = "CC",
            ["numero_documento"] = $"CC{unique}",
            ["country_id"] = location.CountryId,
            ["department_id"] = location.DepartmentId,
            ["municipality_id"] = location.MunicipalityId
        };

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = $"ana_{unique}",
            password = "Password1!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginPayload.GetProperty("access_token").GetString();

        accessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var apuResponse = await client.GetAsync("/api/v1/apu/public");

        apuResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Administrator_Is_Denied_From_Public_Apu_Read_Endpoint()
    {
        using var factory = new TestWebApplicationFactory();

        var adminUser = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, "ADMINISTRADOR")
            .Build();

        var response = await client.GetAsync("/api/v1/apu/public");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
