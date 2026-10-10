using BudgetCivil.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Param;

public sealed class MunicipalityConfiguration : IEntityTypeConfiguration<Municipality>
{
    public void Configure(EntityTypeBuilder<Municipality> builder)
    {
        builder.ToTable("municipalities");

        builder.HasKey(municipality => municipality.Id);

        builder.Property(municipality => municipality.Id)
            .HasColumnName("id");

        builder.Property(municipality => municipality.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(municipality => municipality.Code)
            .HasColumnName("code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(municipality => municipality.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(municipality => municipality.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(municipality => new { municipality.DepartmentId, municipality.Code })
            .IsUnique();

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(municipality => municipality.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}