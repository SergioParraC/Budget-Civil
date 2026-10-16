using System.Net;
using System.Net.Http.Json;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using FluentAssertions;

namespace BudgetCivil.Backend.BlackBoxTests.Features.Authentication;

public sealed class LockoutFlowTests
{
    [Fact]
    public async Task Login_Locks_Account_After_Five_Failed_Attempts()
    {
        using var factory = new TestWebApplicationFactory();
        var client = factory.CreateClient();

        var seededUser = await TestDataSeeder.SeedUserAsync(factory, "PRESUPUESTADOR", "Password1!");

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
            {
                identifier = seededUser.Username,
                password = "InvalidPassword1!"
            });

            if (attempt < 5)
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            }
            else
            {
                response.StatusCode.Should().Be((HttpStatusCode)423);
            }
        }

        var loginAfterLockoutResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            identifier = seededUser.Username,
            password = seededUser.Password
        });

        loginAfterLockoutResponse.StatusCode.Should().Be((HttpStatusCode)423);

        var lockedUser = await TestDataSeeder.GetUserAsync(factory, seededUser.UserId);
        lockedUser.Should().NotBeNull();
        lockedUser!.AccountLocked.Should().BeTrue();
        lockedUser.FailedLoginAttempts.Should().BeGreaterThanOrEqualTo(5);
    }
}
