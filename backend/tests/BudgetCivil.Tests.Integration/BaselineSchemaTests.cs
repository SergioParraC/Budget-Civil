using BudgetCivil.Infrastructure.Persistence;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace BudgetCivil.Tests.Integration;

/// <summary>T-I-24: la baseline aplicada crea el esquema reconciliado de `E-1b`.</summary>
public sealed class BaselineSchemaTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    public const string BaselineMigrationId = "202610300438_e1b_reconciliacion_baseline";

    [Fact]
    public async Task La_baseline_esta_aplicada_y_el_esquema_no_conserva_tablas_obsoletas()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains(BaselineMigrationId, appliedMigrations);

        var tables = await ReadTableNamesAsync(context);

        Assert.Contains("users", tables);
        Assert.Contains("projects", tables);
        Assert.Contains("project_configurations", tables);
        Assert.Contains("project_members", tables);
        Assert.Contains("project_status_transitions", tables);
        Assert.Contains("project_visibility_transitions", tables);
        Assert.Contains("role_permissions", tables);

        // D-78, D-98: no quedan tablas del modelo anterior.
        Assert.DoesNotContain("user_company_memberships", tables);
        Assert.DoesNotContain("project_user_memberships", tables);
        Assert.DoesNotContain("user_profiles", tables);
    }

    [Fact]
    public async Task La_configuracion_guarda_la_divisa_el_tope_y_el_AIU_fuera_del_proyecto()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var projectColumns = await ReadColumnNamesAsync(context, "projects");
        Assert.DoesNotContain("base_currency_id", projectColumns);
        Assert.DoesNotContain("aiu_admin_percent", projectColumns);
        Assert.DoesNotContain("signing_mode", projectColumns);

        var configurationColumns = await ReadColumnNamesAsync(context, "project_configurations");
        Assert.Contains("project_id", configurationColumns);
        Assert.Contains("base_currency_id", configurationColumns);
        Assert.Contains("budget_limit", configurationColumns);
        Assert.Contains("aiu_admin_percent", configurationColumns);
        Assert.Contains("signature_mode", configurationColumns);
        Assert.Contains("requires_auditor", configurationColumns);
    }

    private static async Task<List<string>> ReadTableNamesAsync(BudgetCivilDbContext context)
    {
        return await ReadStringsAsync(
            context,
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'");
    }

    private static async Task<List<string>> ReadColumnNamesAsync(BudgetCivilDbContext context, string tableName)
    {
        return await ReadStringsAsync(
            context,
            $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'");
    }

    private static async Task<List<string>> ReadStringsAsync(BudgetCivilDbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        var mustClose = connection.State != System.Data.ConnectionState.Open;

        if (mustClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var values = new List<string>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetString(0));
            }

            return values;
        }
        finally
        {
            if (mustClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
