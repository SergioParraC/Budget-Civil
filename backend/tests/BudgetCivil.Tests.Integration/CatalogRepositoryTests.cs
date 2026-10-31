using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Infrastructure.Persistence.Repositories;
using BudgetCivil.Tests.Integration.Infrastructure;

namespace BudgetCivil.Tests.Integration;

/// <summary>
/// Los catálogos que consume `GET /api/v1/catalogs/*` se leen de las tablas `countries`,
/// `departments`, `municipalities`, `base_currencies` y `document_types`, solo con registros activos.
/// </summary>
public sealed class CatalogRepositoryTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Los_catalogos_se_leen_de_la_base_con_filtros_y_solo_activos()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        Guid countryId;
        Guid inactiveCountryId;
        Guid departmentId;
        Guid municipalityId;
        Guid currencyId;
        Guid documentTypeId;

        await using (var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var country = new Country { Id = Guid.NewGuid(), Code = $"C{suffix}", Name = $"Pais {suffix}" };
            var inactiveCountry = new Country
            {
                Id = Guid.NewGuid(),
                Code = $"I{suffix}",
                Name = $"Inactivo {suffix}",
                IsActive = false
            };
            var department = new Department
            {
                Id = Guid.NewGuid(),
                CountryId = country.Id,
                Code = suffix,
                Name = $"Departamento {suffix}"
            };
            var municipality = new Municipality
            {
                Id = Guid.NewGuid(),
                DepartmentId = department.Id,
                Code = suffix,
                Name = $"Municipio {suffix}"
            };
            var currency = new BaseCurrency
            {
                Id = Guid.NewGuid(),
                IsoCode = suffix,
                Name = $"Moneda {suffix}"
            };
            var documentType = new DocumentType
            {
                Id = Guid.NewGuid(),
                Code = $"D{suffix}",
                Name = $"Documento {suffix}"
            };

            context.AddRange(country, inactiveCountry, department, municipality, currency, documentType);
            await context.SaveChangesAsync();

            countryId = country.Id;
            inactiveCountryId = inactiveCountry.Id;
            departmentId = department.Id;
            municipalityId = municipality.Id;
            currencyId = currency.Id;
            documentTypeId = documentType.Id;
        }

        await using var readContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var repository = new GeographicCatalogRepository(readContext);

        var countries = await repository.GetCountriesAsync(suffix, 0, 10);
        Assert.Contains(countries.Items, country => country.Id == countryId);
        Assert.DoesNotContain(countries.Items, country => country.Id == inactiveCountryId);
        Assert.Equal(1, countries.Total);

        var departments = await repository.GetDepartmentsAsync(countryId, null, 0, 10);
        Assert.Contains(departments.Items, department => department.Id == departmentId);
        Assert.Equal(1, departments.Total);

        var municipalities = await repository.GetMunicipalitiesAsync(departmentId, null, 0, 10);
        Assert.Contains(municipalities.Items, municipality => municipality.Id == municipalityId);
        Assert.Equal(1, municipalities.Total);

        var currencies = await repository.GetBaseCurrenciesAsync(suffix, 0, 10);
        Assert.Contains(currencies.Items, currency => currency.Id == currencyId);
        Assert.Equal(1, currencies.Total);

        var documentTypes = await repository.GetDocumentTypesAsync($"D{suffix}", 0, 10);
        Assert.Contains(documentTypes.Items, documentType => documentType.Id == documentTypeId);
        Assert.Equal(1, documentTypes.Total);
    }
}
