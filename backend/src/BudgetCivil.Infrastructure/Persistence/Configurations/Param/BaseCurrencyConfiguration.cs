using BudgetCivil.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Param;

public sealed class BaseCurrencyConfiguration : IEntityTypeConfiguration<BaseCurrency>
{
    public void Configure(EntityTypeBuilder<BaseCurrency> builder)
    {
        builder.ToTable("base_currencies");

        builder.HasKey(currency => currency.Id);

        builder.Property(currency => currency.Id)
            .HasColumnName("id");

        builder.Property(currency => currency.IsoCode)
            .HasColumnName("iso_code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(currency => currency.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(currency => currency.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(currency => currency.IsoCode)
            .IsUnique();
    }
}