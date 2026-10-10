using BudgetCivil.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Param;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(department => department.Id);

        builder.Property(department => department.Id)
            .HasColumnName("id");

        builder.Property(department => department.CountryId)
            .HasColumnName("country_id")
            .IsRequired();

        builder.Property(department => department.Code)
            .HasColumnName("code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(department => department.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(department => department.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(department => new { department.CountryId, department.Code })
            .IsUnique();

        builder.HasOne<Country>()
            .WithMany()
            .HasForeignKey(department => department.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}