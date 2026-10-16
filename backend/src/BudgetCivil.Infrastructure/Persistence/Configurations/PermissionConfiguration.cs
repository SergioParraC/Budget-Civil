using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Id)
            .HasColumnName("id");

        builder.Property(permission => permission.PermissionKey)
            .HasColumnName("permission_key")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(permission => permission.Description)
            .HasColumnName("description")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(permission => permission.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(permission => permission.SeedVersion)
            .HasColumnName("seed_version")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(permission => permission.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(permission => permission.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(permission => permission.PermissionKey)
            .IsUnique();
    }
}
