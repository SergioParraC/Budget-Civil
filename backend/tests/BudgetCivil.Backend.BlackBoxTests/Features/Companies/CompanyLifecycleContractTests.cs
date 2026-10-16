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

public sealed class CompanyLifecycleContractTests
{
    [Fact]
    public async Task Company_Lifecycle_Create_Update_Disable_Enable_And_List_Works_For_Administrador()
    {
        using var factory = new TestWebApplicationFactory();

        var adminUser = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var location = await TestDataSeeder.SeedLocationAsync(factory);
        var createPayload = new
        {
            business_name = $"Constructora {Feature002TestData.UniqueSuffix()}",
            tax_id = Feature002TestData.UniqueTaxId("LC"),
            is_active = true,
            company_type = "PUBLICA",
            territorial_level = "DEPARTAMENTAL",
            country_id = location.CountryId,
            department_id = location.DepartmentId,
            municipality_id = location.MunicipalityId,
            representative = new
            {
                mode = "MANUAL_DATA",
                full_name = "Representante Prueba",
                document_number = "123456789"
            }
        };

        var createResponse = await client.PostAsJsonAsync("/api/v1/companies", createPayload);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var companyId = createBody.GetProperty("company_id").GetGuid();

        var updatePayload = new
        {
            business_name = $"Constructora Editada {Feature002TestData.UniqueSuffix()}",
            tax_id = Feature002TestData.UniqueTaxId("LE"),
            company_type = "PRIVADA",
            country_id = location.CountryId,
            department_id = location.DepartmentId,
            municipality_id = location.MunicipalityId,
            representative = new
            {
                mode = "USER_REFERENCE",
                user_id = adminUser.UserId
            }
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/companies/{companyId}", updatePayload);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var disableResponse = await client.PatchAsJsonAsync(
            $"/api/v1/companies/{companyId}/disable",
            new { reason = "Inactivacion para pruebas" });

        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var disabledBody = await disableResponse.Content.ReadFromJsonAsync<JsonElement>();
        disabledBody.GetProperty("company_id").GetGuid().Should().Be(companyId);
        disabledBody.GetProperty("is_active").GetBoolean().Should().BeFalse();

        var enableResponse = await client.PatchAsJsonAsync(
            $"/api/v1/companies/{companyId}/enable",
            new { reason = "Reactivacion para pruebas" });

        enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var enabledBody = await enableResponse.Content.ReadFromJsonAsync<JsonElement>();
        enabledBody.GetProperty("company_id").GetGuid().Should().Be(companyId);
        enabledBody.GetProperty("is_active").GetBoolean().Should().BeTrue();

        var listResponse = await client.GetAsync("/api/v1/companies?pageNumber=1&pageSize=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listBody = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        listBody.GetProperty("items").EnumerateArray().Any(item => item.GetProperty("company_id").GetGuid() == companyId)
            .Should().BeTrue();

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var persistedCompany = await dbContext.Companies.SingleAsync(company => company.Id == companyId);
        persistedCompany.IsActive.Should().BeTrue();
        persistedCompany.TaxId.Should().Be(updatePayload.tax_id);
        persistedCompany.CompanyType.Should().Be(updatePayload.company_type);
        persistedCompany.CountryId.Should().Be(location.CountryId);
        persistedCompany.DepartmentId.Should().Be(location.DepartmentId);
        persistedCompany.MunicipalityId.Should().Be(location.MunicipalityId);
        persistedCompany.RepresentativeMode.Should().Be("USER_REFERENCE");
        persistedCompany.RepresentativeUserId.Should().Be(adminUser.UserId);
        persistedCompany.CreatedByUserId.Should().Be(adminUser.UserId);
        persistedCompany.LastModifiedByUserId.Should().Be(adminUser.UserId);
    }

    [Fact]
    public async Task UpdateCompany_Manager_Cannot_Update_Another_Company()
    {
        using var factory = new TestWebApplicationFactory();

        var ownCompanyId = await SeedCompanyAsync(factory, "Own Company");
        var otherCompanyId = await SeedCompanyAsync(factory, "Other Company");
        var managerUser = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: ownCompanyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(managerUser.UserId, Feature002TestData.Roles.DirectorProyectos, ownCompanyId)
            .Build();
        var location = await TestDataSeeder.SeedLocationAsync(factory);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/companies/{otherCompanyId}",
            new
            {
                business_name = "Intento Invalido",
                tax_id = Feature002TestData.UniqueTaxId("XX"),
                company_type = "PRIVADA",
                country_id = location.CountryId,
                department_id = location.DepartmentId,
                municipality_id = location.MunicipalityId,
                representative = new
                {
                    mode = "USER_REFERENCE",
                    user_id = managerUser.UserId
                }
            });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCompanies_Is_Forbidden_For_Non_Administrador()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Scope Company");
        var managerUser = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(managerUser.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var response = await client.GetAsync("/api/v1/companies?pageNumber=1&pageSize=10");

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
            TaxId = $"TL{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
