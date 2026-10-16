using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Security;

public sealed class BootstrapFirstAdministratorTests
{
    [Fact]
    public async Task Bootstrap_Creates_First_Administrator_When_None_Exists()
    {
        using var factory = new Infrastructure.TestWebApplicationFactory();
        var client = factory.CreateClient();
        var location = await Infrastructure.TestDataSeeder.SeedLocationAsync(factory);

        var unique = Guid.NewGuid().ToString("N")[..8];
        var companyTaxId = $"TAX{unique}";
        var request = new
        {
            admin_profile = new
            {
                primer_nombre = "Ada",
                primer_apellido = "Lovelace",
                tipo_documento = "CC",
                numero_documento = $"AD{unique}",
                email = $"admin.{unique}@mail.local",
                username = $"admin_{unique}",
                password = "Password1!",
                country_id = location.CountryId,
                department_id = location.DepartmentId,
                municipality_id = location.MunicipalityId
            },
            company = new
            {
                business_name = $"Bootstrap Company {unique}",
                tax_id = companyTaxId,
                company_type = "PRIVADA",
                country_id = location.CountryId,
                department_id = location.DepartmentId,
                municipality_id = location.MunicipalityId,
                representative = new
                {
                    mode = "MANUAL_DATA",
                    full_name = "Bootstrap Representative",
                    document_number = unique
                }
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/bootstrap/first-administrator", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("bootstrap_enabled").GetBoolean().Should().BeFalse();
        payload.GetProperty("administrator_created").GetBoolean().Should().BeTrue();
        var administratorId = payload.GetProperty("administrator_id").GetGuid();
        administratorId.Should().NotBeEmpty();
        payload.GetProperty("audit_event_id").GetGuid().Should().NotBeEmpty();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var adminsCount = await dbContext.UserAccounts
            .CountAsync(user => user.Role.Code == "ADMINISTRADOR");

        adminsCount.Should().Be(1);

        var administrator = await dbContext.UserAccounts
            .Include(user => user.Profile)
            .SingleAsync(user => user.Id == administratorId);
        administrator.Profile!.CountryId.Should().Be(location.CountryId);
        administrator.Profile.DepartmentId.Should().Be(location.DepartmentId);
        administrator.Profile.MunicipalityId.Should().Be(location.MunicipalityId);

        var company = await dbContext.Companies.SingleAsync(item => item.TaxId == companyTaxId);
        company.CountryId.Should().Be(location.CountryId);
        company.DepartmentId.Should().Be(location.DepartmentId);
        company.MunicipalityId.Should().Be(location.MunicipalityId);
        company.CreatedByUserId.Should().Be(administratorId);
    }

    [Fact]
    public async Task Bootstrap_Is_Idempotent_And_Rejects_When_Administrator_Already_Exists()
    {
        using var factory = new Infrastructure.TestWebApplicationFactory();
        var client = factory.CreateClient();
        var location = await Infrastructure.TestDataSeeder.SeedLocationAsync(factory);

        var uniqueA = Guid.NewGuid().ToString("N")[..8];
        var firstRequest = new
        {
            admin_profile = new
            {
                primer_nombre = "Grace",
                primer_apellido = "Hopper",
                tipo_documento = "CC",
                numero_documento = $"GR{uniqueA}",
                email = $"grace.{uniqueA}@mail.local",
                username = $"grace_{uniqueA}",
                password = "Password1!",
                country_id = location.CountryId,
                department_id = location.DepartmentId,
                municipality_id = location.MunicipalityId
            },
            company = new
            {
                business_name = $"Bootstrap Company A {uniqueA}",
                tax_id = $"TAXA{uniqueA}",
                company_type = "PRIVADA",
                country_id = location.CountryId,
                department_id = location.DepartmentId,
                municipality_id = location.MunicipalityId,
                representative = new
                {
                    mode = "MANUAL_DATA",
                    full_name = "Bootstrap Representative",
                    document_number = uniqueA
                }
            }
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v1/admin/bootstrap/first-administrator", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var uniqueB = Guid.NewGuid().ToString("N")[..8];
        var secondRequest = new
        {
            admin_profile = new
            {
                primer_nombre = "Alan",
                primer_apellido = "Turing",
                tipo_documento = "CC",
                numero_documento = $"AL{uniqueB}",
                email = $"alan.{uniqueB}@mail.local",
                username = $"alan_{uniqueB}",
                password = "Password1!",
                country_id = location.CountryId,
                department_id = location.DepartmentId,
                municipality_id = location.MunicipalityId
            },
            company = new
            {
                business_name = $"Bootstrap Company B {uniqueB}",
                tax_id = $"TAXB{uniqueB}",
                company_type = "PRIVADA",
                country_id = location.CountryId,
                department_id = location.DepartmentId,
                municipality_id = location.MunicipalityId,
                representative = new
                {
                    mode = "MANUAL_DATA",
                    full_name = "Bootstrap Representative",
                    document_number = uniqueB
                }
            }
        };

        var secondResponse = await client.PostAsJsonAsync("/api/v1/admin/bootstrap/first-administrator", secondRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var adminsCount = await dbContext.UserAccounts
            .CountAsync(user => user.Role.Code == "ADMINISTRADOR");

        adminsCount.Should().Be(1);
    }
}
