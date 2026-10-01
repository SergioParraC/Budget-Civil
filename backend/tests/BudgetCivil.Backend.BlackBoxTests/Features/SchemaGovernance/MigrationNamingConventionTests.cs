using System.Text.RegularExpressions;
using FluentAssertions;

namespace BudgetCivil.Backend.BlackBoxTests.Features.SchemaGovernance;

public sealed class MigrationNamingConventionTests
{
    private static readonly Regex Feature002MigrationNameRegex = new(
        @"^\d{12}_[a-z0-9]+_[a-z0-9_]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex GenericMigrationPrefixRegex = new(
        @"^\d{12,14}_.+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Feature002_Migrations_Follow_Required_Name_Format()
    {
        var migrationsDirectory = ResolveMigrationsDirectory();

        var feature002Migrations = Directory
            .GetFiles(migrationsDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Cast<string>()
            .Where(name => !name.EndsWith(".Designer", StringComparison.Ordinal))
            .Where(name => !name.Equals("BudgetCivilDbContextModelSnapshot", StringComparison.Ordinal))
            .Where(name => name.Contains("_feature002_", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        feature002Migrations.Should().NotBeEmpty();
        feature002Migrations.Should().OnlyContain(name => Feature002MigrationNameRegex.IsMatch(name));
        feature002Migrations.Should().Contain("202610010900_feature002_schema_baseline");
    }

    [Fact]
    public void Migrations_Are_Chronological_And_Latest_Is_Feature002_Baseline()
    {
        var migrationsDirectory = ResolveMigrationsDirectory();

        var migrationNames = Directory
            .GetFiles(migrationsDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null)
            .Cast<string>()
            .Where(name => !name.EndsWith(".Designer", StringComparison.Ordinal))
            .Where(name => !name.Equals("BudgetCivilDbContextModelSnapshot", StringComparison.Ordinal))
            .ToArray();

        migrationNames.Should().NotBeEmpty();
        migrationNames.Should().OnlyContain(name => GenericMigrationPrefixRegex.IsMatch(name));

        var orderedByTimestamp = migrationNames
            .Select(name => new { Name = name, Timestamp = ParseTimestamp(name) })
            .OrderBy(item => item.Timestamp)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();

        var lastMigration = orderedByTimestamp[^1];
        lastMigration.Name.Should().Be("202610010900_feature002_schema_baseline");
    }

    private static DateTime ParseTimestamp(string migrationName)
    {
        var timestamp = migrationName.Split('_', 2, StringSplitOptions.TrimEntries)[0];

        return timestamp.Length switch
        {
            12 => DateTime.ParseExact(timestamp, "yyyyMMddHHmm", null),
            14 => DateTime.ParseExact(timestamp, "yyyyMMddHHmmss", null),
            _ => throw new InvalidOperationException($"Unsupported migration timestamp format: {migrationName}")
        };
    }

    private static string ResolveMigrationsDirectory()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "src",
                "BudgetCivil.Infrastructure",
                "Persistence",
                "Migrations");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the migrations directory from test runtime path.");
    }
}
