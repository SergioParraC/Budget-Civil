using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using FluentAssertions;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Listings;

public sealed class CatalogListingsContractTests
{
    [Fact]
    public async Task Countries_Listing_Returns_Filtered_Paginated_Items()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var response = await client.GetAsync("/api/v1/catalogs/countries?search=co&pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        payload.GetProperty("page_number").GetInt32().Should().Be(1);
        payload.GetProperty("page_size").GetInt32().Should().Be(10);

        var items = payload.GetProperty("items").EnumerateArray().ToList();
        items.Should().NotBeEmpty();

        foreach (var item in items)
        {
            item.GetProperty("id").GetGuid().Should().NotBeEmpty();
            item.GetProperty("code").GetString().Should().NotBeNullOrWhiteSpace();
            item.GetProperty("name").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Departments_And_Municipalities_Listing_Supports_Parent_Filters()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var countriesResponse = await client.GetAsync("/api/v1/catalogs/countries?search=co&pageNumber=1&pageSize=10");
        countriesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var countriesPayload = await countriesResponse.Content.ReadFromJsonAsync<JsonElement>();
        var countryId = countriesPayload.GetProperty("items")[0].GetProperty("id").GetGuid();

        var departmentsResponse = await client.GetAsync($"/api/v1/catalogs/departments?countryId={countryId}&pageNumber=1&pageSize=10");
        departmentsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var departmentsPayload = await departmentsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var departments = departmentsPayload.GetProperty("items").EnumerateArray().ToList();
        departments.Should().NotBeEmpty();
        departments.All(item => item.GetProperty("country_id").GetGuid() == countryId).Should().BeTrue();

        var departmentId = departments[0].GetProperty("id").GetGuid();

        var municipalitiesResponse = await client.GetAsync($"/api/v1/catalogs/municipalities?departmentId={departmentId}&pageNumber=1&pageSize=10");
        municipalitiesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var municipalitiesPayload = await municipalitiesResponse.Content.ReadFromJsonAsync<JsonElement>();
        var municipalities = municipalitiesPayload.GetProperty("items").EnumerateArray().ToList();
        municipalities.Should().NotBeEmpty();
        municipalities.All(item => item.GetProperty("department_id").GetGuid() == departmentId).Should().BeTrue();
    }

    [Fact]
    public async Task Currencies_And_DocumentTypes_Listing_Is_Filterable()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var currenciesResponse = await client.GetAsync("/api/v1/catalogs/currencies?search=co&pageNumber=1&pageSize=10");
        currenciesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var currenciesPayload = await currenciesResponse.Content.ReadFromJsonAsync<JsonElement>();
        currenciesPayload.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);

        var documentTypesResponse = await client.GetAsync("/api/v1/catalogs/document-types?search=ci&pageNumber=1&pageSize=10");
        documentTypesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var documentTypesPayload = await documentTypesResponse.Content.ReadFromJsonAsync<JsonElement>();
        documentTypesPayload.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }
}
