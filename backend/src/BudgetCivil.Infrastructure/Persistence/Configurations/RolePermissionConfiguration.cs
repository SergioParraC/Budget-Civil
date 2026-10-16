using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rolePermission => rolePermission.Id);

        builder.Property(rolePermission => rolePermission.Id)
            .HasColumnName("id");

        builder.Property(rolePermission => rolePermission.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(rolePermission => rolePermission.PermissionId)
            .HasColumnName("permission_id")
            .IsRequired();

        builder.Property(rolePermission => rolePermission.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(rolePermission => rolePermission.AssignedAtUtc)
            .HasColumnName("assigned_at_utc")
            .IsRequired();

        builder.Property(rolePermission => rolePermission.RevokedAtUtc)
            .HasColumnName("revoked_at_utc");

        builder.Property(rolePermission => rolePermission.SeedVersion)
            .HasColumnName("seed_version")
            .HasMaxLength(20);

        builder.HasIndex(rolePermission => new { rolePermission.RoleId, rolePermission.PermissionId })
            .IsUnique();

        builder.HasOne(rolePermission => rolePermission.Role)
            .WithMany(role => role.RolePermissions)
            .HasForeignKey(rolePermission => rolePermission.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(rolePermission => rolePermission.Permission)
            .WithMany(permission => permission.RolePermissions)
            .HasForeignKey(rolePermission => rolePermission.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
