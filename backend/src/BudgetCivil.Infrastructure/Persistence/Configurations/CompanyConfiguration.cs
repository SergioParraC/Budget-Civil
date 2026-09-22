using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(company => company.Id);

        builder.Property(company => company.Id)
            .HasColumnName("id");

        builder.Property(company => company.BusinessName)
            .HasColumnName("business_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(company => company.TaxId)
            .HasColumnName("tax_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(company => company.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(company => company.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(company => company.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(company => company.TaxId)
            .IsUnique();
    }
}
