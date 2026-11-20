using BudgetCivil.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Catalogs;

public sealed class MeasurementUnitConfiguration : IEntityTypeConfiguration<MeasurementUnit>
{
    public void Configure(EntityTypeBuilder<MeasurementUnit> builder)
    {
        builder.ToTable("measurement_units", table =>
        {
            table.HasCheckConstraint(
                "CK_measurement_units_short_unit_not_empty",
                "LEN(LTRIM(RTRIM(short_unit))) > 0");
            table.HasCheckConstraint(
                "CK_measurement_units_unit_not_empty",
                "LEN(LTRIM(RTRIM(unit))) > 0");
        });

        builder.HasKey(unit => unit.Id);

        builder.Property(unit => unit.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(unit => unit.ShortUnit)
            .HasColumnName("short_unit")
            .HasMaxLength(MeasurementUnit.ShortUnitMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(unit => unit.Unit)
            .HasColumnName("unit")
            .HasMaxLength(MeasurementUnit.UnitMaxLength)
            .IsUnicode(false)
            .IsRequired();

        // La convención de nombres de `SchemaNamingConventions` fija el nombre definitivo con prefijo `dbo`.
        builder.HasIndex(unit => unit.ShortUnit)
            .IsUnique()
            .HasDatabaseName("UQ_measurement_units_short_unit");
    }
}