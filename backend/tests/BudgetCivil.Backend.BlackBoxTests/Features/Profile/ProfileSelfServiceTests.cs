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

public sealed class ProfileSelfServiceTests
{
    [Theory]
    [InlineData("ADMINISTRADOR")]
    [InlineData("GERENTE_PROYECTOS")]
    [InlineData("PRESUPUESTADOR")]
    [InlineData("AUDITOR")]
    public async Task GetMyProfile_Returns_Authenticated_User_Profile_For_All_Roles(string roleCode)
    {
        using var factory = new TestWebApplicationFactory();

        Guid? companyId = null;
        if (roleCode != "ADMINISTRADOR")
        {
            companyId = await SeedCompanyAsync(factory, "Profile Role Company");
        }

        var seededUser = await TestDataSeeder.SeedUserAsync(factory, roleCode, "Password1!", companyId: companyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(seededUser.UserId, roleCode, companyId)
            .Build();

        var response = await client.GetAsync("/api/v1/profile/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("user_id").GetGuid().Should().Be(seededUser.UserId);
        payload.GetProperty("email").GetString().Should().Be(seededUser.Email);
        payload.GetProperty("username").GetString().Should().Be(seededUser.Username);
        payload.GetProperty("role_code").GetString().Should().Be(roleCode);
    }

    [Fact]
    public async Task UpdateMyProfile_Updates_Only_Authenticated_User_Data()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Profile Update Company");
        var actor = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyId);
        var otherUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(actor.UserId, "PRESUPUESTADOR", companyId)
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            primer_nombre = "Luisa",
            segundo_nombre = "Maria",
            primer_apellido = "Mejia",
            segundo_apellido = "Arango",
            tipo_documento = "CC",
            numero_documento = $"SELF-{unique}",
            fecha_nacimiento = "1993-07-21",
            residencia = new
            {
                municipio = "Cali",
                departamento = "Valle del Cauca",
                pais = "Colombia"
            },
            email = $"self.{unique}@mail.local",
            username = $"self_{unique}",
            tarjeta_profesional = "TP-4455",
            fecha_expedicion_tarjeta_profesional = "2018-03-12",
            archivo_tarjeta_profesional = "tp/self.pdf"
        };

        var response = await client.PutAsJsonAsync("/api/v1/profile/me", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responsePayload = await response.Content.ReadFromJsonAsync<JsonElement>();
        responsePayload.GetProperty("user_id").GetGuid().Should().Be(actor.UserId);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var actorUser = await dbContext.UserAccounts
            .Include(user => user.Profile)
            .SingleAsync(user => user.Id == actor.UserId);

        actorUser.Email.Should().Be(payload.email);
        actorUser.Username.Should().Be(payload.username);
        actorUser.Profile!.FirstName.Should().Be(payload.primer_nombre);
        actorUser.Profile.LastName.Should().Be(payload.primer_apellido);
        actorUser.Profile.ResidenceCity.Should().Be(payload.residencia.municipio);

        var unaffectedUser = await dbContext.UserAccounts
            .Include(user => user.Profile)
            .SingleAsync(user => user.Id == otherUser.UserId);

        unaffectedUser.Email.Should().NotBe(payload.email);
        unaffectedUser.Username.Should().NotBe(payload.username);
        unaffectedUser.Profile!.DocumentNumber.Should().NotBe(payload.numero_documento);
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
            TaxId = $"TP{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
