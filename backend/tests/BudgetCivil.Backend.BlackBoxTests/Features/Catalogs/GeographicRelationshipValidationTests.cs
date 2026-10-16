using System.Net;
using System.Net.Http.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Catalogs;

public sealed class GeographicRelationshipValidationTests
{
    [Fact]
    public async Task Register_Rejects_Municipality_From_Another_Department()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();
        var location = await TestDataSeeder.SeedLocationAsync(factory);
        var otherDepartmentId = Guid.NewGuid();
        var otherMunicipalityId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();
            dbContext.Departments.Add(new Department
            {
                Id = otherDepartmentId,
                CountryId = location.CountryId,
                Code = "OTHER-DEP",
                Name = "Other Department",
                IsActive = true
            });
            dbContext.Municipalities.Add(new Municipality
            {
                Id = otherMunicipalityId,
                DepartmentId = otherDepartmentId,
                Code = "OTHER-MUN",
                Name = "Other Municipality",
                IsActive = true
            });
            await dbContext.SaveChangesAsync();
        }

        var unique = Guid.NewGuid().ToString("N")[..10];
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            primer_nombre = "Ana",
            primer_apellido = "Garcia",
            email = $"geo.{unique}@mail.local",
            username = $"geo_{unique}",
            password = "Password1!",
            tipo_documento = "CC",
            numero_documento = $"GEO-{unique}",
            country_id = location.CountryId,
            department_id = location.DepartmentId,
            municipality_id = otherMunicipalityId
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCompany_Rejects_Inactive_Geographic_Catalog()
    {
        using var factory = new TestWebApplicationFactory();
        var location = await TestDataSeeder.SeedLocationAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();
            var country = await dbContext.Countries.FindAsync(location.CountryId);
            country!.IsActive = false;
            await dbContext.SaveChangesAsync();
        }

        var admin = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");
        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, "ADMINISTRADOR")
            .Build();

        var unique = Guid.NewGuid().ToString("N")[..10];
        var response = await client.PostAsJsonAsync("/api/v1/companies", new
        {
            business_name = $"Inactive Catalog Company {unique}",
            tax_id = $"GEO{unique}",
            company_type = "PRIVADA",
            country_id = location.CountryId,
            department_id = location.DepartmentId,
            municipality_id = location.MunicipalityId,
            representative = new
            {
                mode = "MANUAL_DATA",
                full_name = "Test Representative",
                document_number = unique
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
