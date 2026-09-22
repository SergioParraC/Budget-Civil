using BudgetCivil.Domain.Users.Entities;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BudgetCivilDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
