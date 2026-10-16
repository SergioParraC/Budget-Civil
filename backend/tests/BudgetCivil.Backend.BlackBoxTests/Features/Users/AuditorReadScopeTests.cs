using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Users;

public sealed class AuditorReadScopeTests
{
    [Fact]
    public async Task GetUsers_Allows_Auditor_Read_Within_Own_Company_Only()
    {
        using var factory = new TestWebApplicationFactory();

        var companyA = await SeedCompanyAsync(factory, "Auditor A");
        var companyB = await SeedCompanyAsync(factory, "Auditor B");

        var auditor = await TestDataSeeder.SeedUserAsync(factory, "AUDITOR", "Password1!", companyId: companyA);
        _ = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyA);
        var foreign = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!", companyId: companyB);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(auditor.UserId, "AUDITOR", companyA)
            .Build();

        var response = await client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.ValueKind.Should().Be(JsonValueKind.Array);

        var allRows = payload.EnumerateArray().ToList();
        allRows.Should().NotBeEmpty();
        allRows.All(row => row.GetProperty("company_id").GetGuid() == companyA).Should().BeTrue();

        var emails = allRows
            .Select(row => row.GetProperty("email").GetString())
            .Where(value => value is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        emails.Should().NotContain(foreign.Email);
    }

    [Fact]
    public async Task Auditor_Cannot_Use_Write_Endpoints_And_Gets_Forbidden()
    {
        using var factory = new TestWebApplicationFactory();

        var companyA = await SeedCompanyAsync(factory, "Auditor Write");
        var auditor = await TestDataSeeder.SeedUserAsync(factory, "AUDITOR", "Password1!", companyId: companyA);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(auditor.UserId, "AUDITOR", companyA)
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var location = await TestDataSeeder.SeedLocationAsync(factory);
        var payload = new
        {
            primer_nombre = "Read",
            primer_apellido = "Only",
            tipo_documento = "CC",
            numero_documento = $"AUD-{unique}",
            fecha_nacimiento = "1992-01-20",
            residencia = new
            {
                country_id = location.CountryId,
                department_id = location.DepartmentId,
                municipality_id = location.MunicipalityId
            },
            email = $"auditor.write.{unique}@mail.local",
            username = $"auditor_write_{unique}",
            password = "Password1!"
        };

        var response = await client.PostAsJsonAsync("/api/v1/users", payload);

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
            TaxId = $"AU{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
