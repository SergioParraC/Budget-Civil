using BudgetCivil.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BudgetCivil.Tests.Integration.Infrastructure;

/// <summary>
/// Aplica la baseline sobre la base vacía y siembra la línea base de seguridad una sola vez por ejecución.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        ConnectionString = SqlServerTestDatabase.ResolveConnectionString();

        await using var context = SqlServerTestDatabase.CreateContext(ConnectionString);
        await context.Database.MigrateAsync();

        var seeder = new SecurityBaselineSeeder(context, NullLogger<SecurityBaselineSeeder>.Instance);
        await seeder.SeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
