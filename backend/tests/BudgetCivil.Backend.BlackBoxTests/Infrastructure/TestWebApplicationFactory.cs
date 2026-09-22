using BudgetCivil.Infrastructure.Persistence;
using BudgetCivil.Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BudgetCivil.Backend.BlackBoxTests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"BudgetCivilTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:BudgetCivilDatabase"] = "Server=(localdb)\\MSSQLLocalDB;Database=BudgetCivilDb.Tests;Trusted_Connection=True;TrustServerCertificate=True",
                ["Jwt:Issuer"] = "BudgetCivil.Api",
                ["Jwt:Audience"] = "BudgetCivil.Client",
                ["Jwt:SigningKey"] = "CHANGE_ME_WITH_A_MINIMUM_32_CHAR_SECRET",
                ["Jwt:ExpirationMinutes"] = "120"
            };

            configurationBuilder.AddInMemoryCollection(testSettings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<BudgetCivilDbContext>));
            services.RemoveAll(typeof(BudgetCivilDbContext));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<BudgetCivilDbContext>));

            services.AddDbContext<BudgetCivilDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            using var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();
            dbContext.Database.EnsureCreated();

            var seeder = scope.ServiceProvider.GetRequiredService<ISecurityBaselineSeeder>();
            seeder.SeedAsync().GetAwaiter().GetResult();
        });
    }
}
