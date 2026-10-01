using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Listings;

public sealed class PaginationValidationTests
{
    [Fact]
    public async Task Companies_Listing_Rejects_Invalid_PageSize()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var response = await client.GetAsync("/api/v1/companies?pageNumber=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertPageSizeInvalidAsync(response);
    }

    [Fact]
    public async Task Projects_Listing_Rejects_Invalid_PageSize()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var response = await client.GetAsync("/api/v1/projects?pageNumber=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertPageSizeInvalidAsync(response);
    }

    [Fact]
    public async Task Catalogs_Listing_Rejects_Invalid_PageSize()
    {
        using var factory = new TestWebApplicationFactory();
        var admin = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.Administrador, "Password1!");

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(admin.UserId, Feature002TestData.Roles.Administrador)
            .Build();

        var response = await client.GetAsync("/api/v1/catalogs/countries?pageNumber=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertPageSizeInvalidAsync(response);
    }

    [Fact]
    public async Task MyCompanyUsers_Listing_Rejects_Invalid_PageSize()
    {
        using var factory = new TestWebApplicationFactory();

        var companyId = await SeedCompanyAsync(factory, "Pagination");
        var director = await TestDataSeeder.SeedUserAsync(factory, Feature002TestData.Roles.DirectorProyectos, "Password1!", companyId: companyId);

        var client = new AuthenticatedClientBuilder(factory)
            .WithUser(director.UserId, Feature002TestData.Roles.DirectorProyectos, companyId)
            .Build();

        var response = await client.GetAsync("/api/v1/users/company/me?pageNumber=1&pageSize=25");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await AssertPageSizeInvalidAsync(response);
    }

    private static async Task AssertPageSizeInvalidAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("code").GetString().Should().Be("PAGE_SIZE_INVALID");
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
            TaxId = $"PG{token}"[..12],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }
}
