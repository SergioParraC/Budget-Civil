using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.SchemaGovernance;

public sealed class ForeignKeyDirectionRulesTests
{
    [Fact]
    public void Model_ForeignKeys_Respect_Allowed_Schema_Directions()
    {
        using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var violations = new List<string>();

        foreach (var entityType in dbContext.Model.GetEntityTypes())
        {
            var fromTable = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(fromTable))
            {
                continue;
            }

            var fromSchema = NormalizeSchema(entityType.GetSchema());

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var principalEntityType = foreignKey.PrincipalEntityType;
                var toTable = principalEntityType.GetTableName();
                if (string.IsNullOrWhiteSpace(toTable))
                {
                    continue;
                }

                var toSchema = NormalizeSchema(principalEntityType.GetSchema());
                if (!IsAllowedDirection(fromSchema, toSchema))
                {
                    violations.Add($"{fromSchema}.{fromTable} -> {toSchema}.{toTable}");
                }
            }
        }

        violations.Should().BeEmpty("all foreign keys must follow FR-049 schema direction rules");
    }

    [Fact]
    public void Model_Has_No_Foreign_Keys_To_Or_From_Log_Schema()
    {
        using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var logViolations = new List<string>();

        foreach (var entityType in dbContext.Model.GetEntityTypes())
        {
            var fromTable = entityType.GetTableName();
            if (string.IsNullOrWhiteSpace(fromTable))
            {
                continue;
            }

            var fromSchema = NormalizeSchema(entityType.GetSchema());

            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                var principalEntityType = foreignKey.PrincipalEntityType;
                var toTable = principalEntityType.GetTableName();
                if (string.IsNullOrWhiteSpace(toTable))
                {
                    continue;
                }

                var toSchema = NormalizeSchema(principalEntityType.GetSchema());
                if (fromSchema == DatabaseSchemas.Log || toSchema == DatabaseSchemas.Log)
                {
                    logViolations.Add($"{fromSchema}.{fromTable} -> {toSchema}.{toTable}");
                }
            }
        }

        logViolations.Should().BeEmpty("log schema must remain isolated from business FKs");
    }

    private static string NormalizeSchema(string? schema)
    {
        return string.IsNullOrWhiteSpace(schema)
            ? DatabaseSchemas.Dbo
            : schema.Trim().ToLowerInvariant();
    }

    private static bool IsAllowedDirection(string fromSchema, string toSchema)
    {
        if (fromSchema == toSchema)
        {
            return true;
        }

        if (fromSchema == DatabaseSchemas.Dbo)
        {
            return toSchema is DatabaseSchemas.Param or DatabaseSchemas.Iam;
        }

        if (fromSchema == DatabaseSchemas.Audit)
        {
            return toSchema is DatabaseSchemas.Dbo or DatabaseSchemas.Iam;
        }

        return false;
    }
}
