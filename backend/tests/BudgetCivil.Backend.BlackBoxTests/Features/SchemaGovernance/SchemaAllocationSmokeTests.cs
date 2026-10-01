using BudgetCivil.Backend.BlackBoxTests.Infrastructure;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Backend.BlackBoxTests.Features.SchemaGovernance;

public sealed class SchemaAllocationSmokeTests
{
    [Fact]
    public void Model_Maps_Entities_To_Expected_Schemas()
    {
        using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();
        var model = dbContext.Model;

        model.GetDefaultSchema().Should().Be(DatabaseSchemas.Dbo);

        GetSchema<Role>(model).Should().Be(DatabaseSchemas.Iam);
        GetSchema<Permission>(model).Should().Be(DatabaseSchemas.Iam);
        GetSchema<RolePermission>(model).Should().Be(DatabaseSchemas.Iam);

        GetSchema<Company>(model).Should().Be(DatabaseSchemas.Dbo);
        GetSchema<UserAccount>(model).Should().Be(DatabaseSchemas.Dbo);
        GetSchema<UserProfile>(model).Should().Be(DatabaseSchemas.Dbo);
        GetSchema<UserCompanyMembership>(model).Should().Be(DatabaseSchemas.Dbo);
        GetSchema<Project>(model).Should().Be(DatabaseSchemas.Dbo);
        GetSchema<ProjectUserMembership>(model).Should().Be(DatabaseSchemas.Dbo);
        GetSchema<PasswordRecoveryGrant>(model).Should().Be(DatabaseSchemas.Dbo);

        GetSchema<SecurityAuditEvent>(model).Should().Be(DatabaseSchemas.Audit);
        GetSchema<ProjectStatusTransition>(model).Should().Be(DatabaseSchemas.Audit);
        GetSchema<ProjectVisibilityTransition>(model).Should().Be(DatabaseSchemas.Audit);

        var schemaMap = model.FindAnnotation("Feature002:SchemaMap")?.Value?.ToString();
        schemaMap.Should().NotBeNullOrWhiteSpace();
        schemaMap.Should().Contain(DatabaseSchemas.Param);
        schemaMap.Should().Contain(DatabaseSchemas.Iam);
        schemaMap.Should().Contain(DatabaseSchemas.Dbo);
        schemaMap.Should().Contain(DatabaseSchemas.Audit);
        schemaMap.Should().Contain(DatabaseSchemas.Log);
    }

    [Fact]
    public void Model_Uses_Only_Allowed_Schemas()
    {
        using var factory = new TestWebApplicationFactory();
        using var scope = factory.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();

        var mappedSchemas = dbContext.Model
            .GetEntityTypes()
            .Where(entityType => !entityType.IsOwned() && !string.IsNullOrWhiteSpace(entityType.GetTableName()))
            .Select(entityType => NormalizeSchema(entityType.GetSchema()))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        mappedSchemas.Should().OnlyContain(schema => DatabaseSchemas.All.Contains(schema, StringComparer.Ordinal));
        mappedSchemas.Should().Contain(DatabaseSchemas.Iam);
        mappedSchemas.Should().Contain(DatabaseSchemas.Dbo);
        mappedSchemas.Should().Contain(DatabaseSchemas.Audit);
    }

    private static string GetSchema<TEntity>(IModel model)
    {
        var entityType = model.FindEntityType(typeof(TEntity));
        entityType.Should().NotBeNull();

        return NormalizeSchema(entityType!.GetSchema());
    }

    private static string NormalizeSchema(string? schema)
    {
        return string.IsNullOrWhiteSpace(schema)
            ? DatabaseSchemas.Dbo
            : schema.Trim().ToLowerInvariant();
    }
}
