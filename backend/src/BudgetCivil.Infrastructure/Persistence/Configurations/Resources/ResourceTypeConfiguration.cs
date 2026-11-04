using BudgetCivil.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Resources;

public sealed class ResourceTypeConfiguration : IEntityTypeConfiguration<ResourceType>
{
    public void Configure(EntityTypeBuilder<ResourceType> builder)
    {
        builder.ToTable("resource_types");

        builder.HasKey(resourceType => resourceType.Id);

        builder.Property(resourceType => resourceType.Id)
            .HasColumnName("id");

        builder.Property(resourceType => resourceType.Code)
            .HasColumnName("code")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(resourceType => resourceType.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(resourceType => resourceType.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(resourceType => resourceType.Code)
            .IsUnique()
            .HasDatabaseName("UQ_resource_types_code");
    }
}
