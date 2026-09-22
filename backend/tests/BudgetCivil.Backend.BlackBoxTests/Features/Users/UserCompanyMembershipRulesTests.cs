using System.Net;
using System.Net.Http.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Users;

public sealed class UserCompanyMembershipRulesTests
{
    [Fact]
    public async Task ChangeCompany_Returns_Conflict_When_User_Has_Active_Membership()
    {
        using var factory = new TestWebApplicationFactory();

        var adminUser = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");
        var sourceCompanyId = await SeedCompanyAsync(factory, "Source Membership Company");
        var destinationCompanyId = await SeedCompanyAsync(factory, "Destination Membership Company");

        var targetUser = await TestDataSeeder.SeedUserAsync(
            factory,
            "PRESUPUESTADOR",
            "Password1!",
            companyId: sourceCompanyId);

        await SeedMembershipAsync(factory, targetUser.UserId, sourceCompanyId, adminUser.UserId, isActive: true);

        var adminClient = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, "ADMINISTRADOR")
            .Build();

        var response = await adminClient.PatchAsJsonAsync(
            $"/api/v1/users/{targetUser.UserId}/company",
            new Dictionary<string, object?>
            {
                ["company_id"] = destinationCompanyId
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var persistedUser = await dbContext.UserAccounts.SingleAsync(user => user.Id == targetUser.UserId);
        persistedUser.CompanyId.Should().Be(sourceCompanyId);

        var activeMemberships = await dbContext.UserCompanyMemberships
            .Where(membership => membership.UserId == targetUser.UserId && membership.IsActive)
            .ToListAsync();

        activeMemberships.Should().ContainSingle();
        activeMemberships[0].CompanyId.Should().Be(sourceCompanyId);
    }

    [Fact]
    public async Task ChangeCompany_Allows_Assignment_When_Only_Inactive_Membership_Exists()
    {
        using var factory = new TestWebApplicationFactory();

        var adminUser = await TestDataSeeder.SeedUserAsync(factory, "ADMINISTRADOR", "Password1!");
        var historicalCompanyId = await SeedCompanyAsync(factory, "Historical Membership Company");
        var destinationCompanyId = await SeedCompanyAsync(factory, "New Membership Company");

        var targetUser = await TestDataSeeder.SeedUserAsync(
            factory,
            "PRESUPUESTADOR",
            "Password1!",
            companyId: null);

        await SeedMembershipAsync(factory, targetUser.UserId, historicalCompanyId, adminUser.UserId, isActive: false);

        var adminClient = new AuthenticatedClientBuilder(factory)
            .WithUser(adminUser.UserId, "ADMINISTRADOR")
            .Build();

        var response = await adminClient.PatchAsJsonAsync(
            $"/api/v1/users/{targetUser.UserId}/company",
            new Dictionary<string, object?>
            {
                ["company_id"] = destinationCompanyId
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var memberships = await dbContext.UserCompanyMemberships
            .Where(membership => membership.UserId == targetUser.UserId)
            .OrderBy(membership => membership.AssignedAtUtc)
            .ToListAsync();

        memberships.Should().HaveCount(2);
        memberships.Count(membership => membership.IsActive).Should().Be(1);
        memberships.Single(membership => membership.IsActive).CompanyId.Should().Be(destinationCompanyId);
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
            TaxId = $"TAX{token}"[..13],
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return companyId;
    }

    private static async Task SeedMembershipAsync(
        TestWebApplicationFactory factory,
        Guid userId,
        Guid companyId,
        Guid assignedByUserId,
        bool isActive)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var assignedAt = DateTime.UtcNow.AddDays(-1);
        dbContext.UserCompanyMemberships.Add(new UserCompanyMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = companyId,
            IsActive = isActive,
            AssignedByUserId = assignedByUserId,
            AssignedAtUtc = assignedAt,
            EndedAtUtc = isActive ? null : assignedAt.AddHours(2)
        });

        await dbContext.SaveChangesAsync();
    }
}
