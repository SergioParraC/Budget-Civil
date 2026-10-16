using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Companies;

public sealed class CompanyAdministrationContractTests
{
    [Fact]
    public async Task CreateCompany_Allows_Administrador_And_Returns_Created()
    {
        using var factory = new TestWebApplicationFactory();

        var adminUser = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");
        var location = await TestDataSeeder.SeedLocationAsync(factory);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, "ADMINISTRADOR")
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var payload = new
        {
            business_name = $"Constructora Admin {unique}",
            tax_id = $"TA{unique}"[..12],
            company_type = "PRIVADA",
            country_id = location.CountryId,
            department_id = location.DepartmentId,
            municipality_id = location.MunicipalityId,
            is_active = true,
            representative = new
            {
                mode = "MANUAL_DATA",
                full_name = "Representante Administrativo",
                document_number = unique
            }
        };

        var response = await client.PostAsJsonAsync("/api/v1/companies", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responsePayload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var companyId = responsePayload.GetProperty("company_id").GetGuid();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var createdCompany = await dbContext.Companies.SingleAsync(company => company.Id == companyId);
        createdCompany.BusinessName.Should().Be(payload.business_name);
        createdCompany.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCompany_Allows_Manager_Only_For_Own_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var ownCompanyId = await SeedCompanyAsync(factory, "Manager Own Company");
        var managerUser = await TestDataSeeder.SeedUserAsync(factory, "GERENTE_PROYECTOS", "Password1!", companyId: ownCompanyId);
        var location = await TestDataSeeder.SeedLocationAsync(factory);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(managerUser.UserId, "GERENTE_PROYECTOS", ownCompanyId)
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var payload = new
        {
            business_name = $"Manager Updated Company {unique}",
            tax_id = $"TB{unique}"[..12],
            company_type = "PRIVADA",
            country_id = location.CountryId,
            department_id = location.DepartmentId,
            municipality_id = location.MunicipalityId,
            representative = new
            {
                mode = "USER_REFERENCE",
                user_id = managerUser.UserId
            }
        };

        var response = await client.PutAsJsonAsync($"/api/v1/companies/{ownCompanyId}", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var updatedCompany = await dbContext.Companies.SingleAsync(company => company.Id == ownCompanyId);
        updatedCompany.BusinessName.Should().Be(payload.business_name);
        updatedCompany.TaxId.Should().Be(payload.tax_id);
    }

    [Fact]
    public async Task DeactivateCompany_Allows_Administrador_And_Soft_Deactivates()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Deactivate Target Company");
        var adminUser = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, "ADMINISTRADOR")
            .Build();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/companies/{companyId}/deactivate",
            new
            {
                motivo = "Cierre operativo"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responsePayload = await response.Content.ReadFromJsonAsync<JsonElement>();
        responsePayload.GetProperty("company_id").GetGuid().Should().Be(companyId);
        responsePayload.GetProperty("is_active").GetBoolean().Should().BeFalse();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var deactivatedCompany = await dbContext.Companies.SingleAsync(company => company.Id == companyId);
        deactivatedCompany.IsActive.Should().BeFalse();
    }

    private static async Task<Guid> SeedCompanyAsync(TestWebApplicationFactory factory, string namePrefix)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var companyId = Guid.NewGuid();
        var token = companyId.ToString("N")[..10].ToUpperInvariant();

        dbContext.Companies.Add(new Company
        {
            Id = companyId,
            BusinessName = $"{namePrefix} {token}",
            TaxId = $"TC{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
