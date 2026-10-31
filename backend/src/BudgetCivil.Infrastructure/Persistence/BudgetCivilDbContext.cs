using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence;

public sealed class BudgetCivilDbContext(DbContextOptions<BudgetCivilDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();

    public DbSet<Country> Countries => Set<Country>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Municipality> Municipalities => Set<Municipality>();

    public DbSet<BaseCurrency> BaseCurrencies => Set<BaseCurrency>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<PasswordRecoveryToken> PasswordRecoveryTokens => Set<PasswordRecoveryToken>();

    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    public DbSet<UserSignature> UserSignatures => Set<UserSignature>();

    public DbSet<PasswordRecoveryGrant> PasswordRecoveryGrants => Set<PasswordRecoveryGrant>();

    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectConfiguration> ProjectConfigurations => Set<ProjectConfiguration>();

    public DbSet<ProjectStatusTransition> ProjectStatusTransitions => Set<ProjectStatusTransition>();

    public DbSet<ProjectVisibilityTransition> ProjectVisibilityTransitions => Set<ProjectVisibilityTransition>();

    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    public DbSet<CompanyMembership> CompanyMemberships => Set<CompanyMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetCivilDbContext).Assembly);

        ConfigureSchemaMappings(modelBuilder);
        SchemaNamingConventions.Apply(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureSchemaMappings(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Permission>().ToTable("permissions");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<RolePermission>().ToTable("role_permissions");
        modelBuilder.Entity<UserSignature>().ToTable("user_signatures");

        modelBuilder.Entity<DocumentType>().ToTable("document_types");
        modelBuilder.Entity<Country>().ToTable("countries");
        modelBuilder.Entity<Department>().ToTable("departments");
        modelBuilder.Entity<Municipality>().ToTable("municipalities");
        modelBuilder.Entity<BaseCurrency>().ToTable("base_currencies");

        modelBuilder.Entity<Company>().ToTable("companies");
        modelBuilder.Entity<UserAccount>().ToTable("users");
        modelBuilder.Entity<ProjectMember>().ToTable("project_members");
        modelBuilder.Entity<PasswordRecoveryGrant>().ToTable("password_recovery_grants");
        modelBuilder.Entity<EmailVerificationToken>().ToTable("email_verification_tokens");
        modelBuilder.Entity<Project>().ToTable("projects");
        modelBuilder.Entity<ProjectConfiguration>().ToTable("project_configurations");

        modelBuilder.Entity<SecurityAuditEvent>().ToTable("security_audit_events");
        modelBuilder.Entity<ProjectStatusTransition>().ToTable("project_status_transitions");
        modelBuilder.Entity<ProjectVisibilityTransition>().ToTable("project_visibility_transitions");
    }
}
