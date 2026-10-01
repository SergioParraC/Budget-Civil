using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Infrastructure.Persistence.Configurations;
using BudgetCivil.Infrastructure.Persistence.SchemaRules;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence;

public sealed class BudgetCivilDbContext(DbContextOptions<BudgetCivilDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<UserCompanyMembership> UserCompanyMemberships => Set<UserCompanyMembership>();

    public DbSet<PasswordRecoveryGrant> PasswordRecoveryGrants => Set<PasswordRecoveryGrant>();

    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectStatusTransition> ProjectStatusTransitions => Set<ProjectStatusTransition>();

    public DbSet<ProjectVisibilityTransition> ProjectVisibilityTransitions => Set<ProjectVisibilityTransition>();

    public DbSet<ProjectUserMembership> ProjectUserMemberships => Set<ProjectUserMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetCivilDbContext).Assembly);

        ConfigureSchemaMappings(modelBuilder);
        SchemaNamingConventions.Apply(modelBuilder);
        ForeignKeyDirectionValidator.Validate(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureSchemaMappings(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DatabaseSchemas.Dbo);
        modelBuilder.HasAnnotation("Feature002:SchemaMap", string.Join(',', DatabaseSchemas.All));

        modelBuilder.Entity<Permission>().ToTable("permissions", DatabaseSchemas.Iam);
        modelBuilder.Entity<Role>().ToTable("roles", DatabaseSchemas.Iam);
        modelBuilder.Entity<RolePermission>().ToTable("role_permissions", DatabaseSchemas.Iam);

        modelBuilder.Entity<Company>().ToTable("companies", DatabaseSchemas.Dbo);
        modelBuilder.Entity<UserAccount>().ToTable("user_accounts", DatabaseSchemas.Dbo);
        modelBuilder.Entity<UserProfile>().ToTable("user_profiles", DatabaseSchemas.Dbo);
        modelBuilder.Entity<UserCompanyMembership>().ToTable("user_company_memberships", DatabaseSchemas.Dbo);
        modelBuilder.Entity<ProjectUserMembership>().ToTable("project_user_memberships", DatabaseSchemas.Dbo);
        modelBuilder.Entity<PasswordRecoveryGrant>().ToTable("password_recovery_grants", DatabaseSchemas.Dbo);
        modelBuilder.Entity<Project>().ToTable("projects", DatabaseSchemas.Dbo);

        modelBuilder.Entity<SecurityAuditEvent>().ToTable("security_audit_events", DatabaseSchemas.Audit);
        modelBuilder.Entity<ProjectStatusTransition>().ToTable("project_status_transitions", DatabaseSchemas.Audit);
        modelBuilder.Entity<ProjectVisibilityTransition>().ToTable("project_visibility_transitions", DatabaseSchemas.Audit);
    }
}
