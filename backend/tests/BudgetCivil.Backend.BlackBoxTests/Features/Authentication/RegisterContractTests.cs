using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Authentication;

public sealed class RegisterContractTests
{
    [Fact]
    public async Task Register_Creates_Presupuestador_With_Null_Company_Defaults()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var payload = new Dictionary<string, object?>
        {
            ["primer_nombre"] = "Laura",
            ["primer_apellido"] = "Gomez",
            ["email"] = $"laura.{unique}@mail.local",
            ["username"] = $"laura_{unique}",
            ["password"] = "Password1!",
            ["tipo_documento"] = "CC",
            ["numero_documento"] = $"CC{unique}"
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responsePayload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var userId = responsePayload.GetProperty("user_id").GetGuid();
        responsePayload.GetProperty("rol_asignado").GetString().Should().Be("Presupuestador");
        responsePayload.GetProperty("company_id").ValueKind.Should().Be(JsonValueKind.Null);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var createdUser = await dbContext.UserAccounts
            .Include(user => user.Role)
            .SingleAsync(user => user.Id == userId);

        createdUser.Role.Code.Should().Be("PRESUPUESTADOR");
        createdUser.CompanyId.Should().BeNull();
    }

    [Theory]
    [InlineData("rol_asignado", "ADMINISTRADOR")]
    [InlineData("company_id", "11111111-1111-1111-1111-111111111111")]
    [InlineData("project_id", "22222222-2222-2222-2222-222222222222")]
    public async Task Register_Rejects_Forbidden_Fields(string fieldName, string fieldValue)
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var payload = new Dictionary<string, object?>
        {
            ["primer_nombre"] = "Juan",
            ["primer_apellido"] = "Perez",
            ["email"] = $"juan.{unique}@mail.local",
            ["username"] = $"juan_{unique}",
            ["password"] = "Password1!",
            ["tipo_documento"] = "CC",
            ["numero_documento"] = $"CC{unique}",
            [fieldName] = fieldValue
        };

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
