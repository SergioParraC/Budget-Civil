using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.SchemaRules;

public static class ForeignKeyDirectionValidator
{
    public static void Validate(ModelBuilder modelBuilder)
    {
        var violations = new List<string>();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
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
                if (IsAllowedDirection(fromSchema, toSchema))
                {
                    continue;
                }

                violations.Add($"{fromSchema}.{fromTable} -> {toSchema}.{toTable}");
            }
        }

        if (violations.Count == 0)
        {
            return;
        }

        var message =
            "Found forbidden foreign key directions by schema. " +
            "Allowed directions are dbo->(param,iam), audit->(dbo,iam), and same-schema references. Violations: " +
            string.Join(", ", violations);

        throw new InvalidOperationException(message);
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
