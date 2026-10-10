using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public static class SchemaNamingConventions
{
    private const int SqlServerMaxIdentifierLength = 128;

    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
            {
                continue;
            }

            var tableName = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                continue;
            }

            var schema = entityType.GetSchema() ?? DatabaseSchemas.Dbo;
            var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());

            var primaryKey = entityType.FindPrimaryKey();
            if (primaryKey is not null)
            {
                primaryKey.SetName(Shorten($"PK_{schema}_{tableName}"));
            }

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var principalTable = foreignKey.PrincipalEntityType.GetTableName();
                if (string.IsNullOrWhiteSpace(principalTable))
                {
                    continue;
                }

                var columnSuffix = string.Join(
                    "_",
                    foreignKey.Properties.Select(property =>
                    {
                        var columnName = property.GetColumnName(storeObject) ?? property.Name;
                        return NormalizeSegment(columnName);
                    }));

                if (string.IsNullOrWhiteSpace(columnSuffix))
                {
                    columnSuffix = "Columns";
                }

                foreignKey.SetConstraintName(Shorten($"FK_{schema}_{tableName}_{principalTable}_{columnSuffix}"));
            }

            foreach (var index in entityType.GetIndexes())
            {
                var columnSuffix = string.Join(
                    "_",
                    index.Properties.Select(property =>
                    {
                        var columnName = property.GetColumnName(storeObject) ?? property.Name;
                        return NormalizeSegment(columnName);
                    }));

                if (string.IsNullOrWhiteSpace(columnSuffix))
                {
                    columnSuffix = "Columns";
                }

                var prefix = index.IsUnique ? "UQ" : "IX";
                index.SetDatabaseName(Shorten($"{prefix}_{schema}_{tableName}_{columnSuffix}"));
            }
        }
    }

    private static string NormalizeSegment(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        var normalized = builder.ToString().Trim('_');
        while (normalized.Contains("__", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("__", "_", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(normalized) ? "Value" : normalized;
    }

    private static string Shorten(string identifier)
    {
        if (identifier.Length <= SqlServerMaxIdentifierLength)
        {
            return identifier;
        }

        using var sha1 = SHA1.Create();
        var hash = Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(identifier)))[..8];
        var prefixLength = SqlServerMaxIdentifierLength - hash.Length - 1;
        return $"{identifier[..prefixLength]}_{hash}";
    }
}
