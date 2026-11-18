using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Apus;

public sealed class ApuResourceConfiguration : IEntityTypeConfiguration<ApuResource>
{
    public void Configure(EntityTypeBuilder<ApuResource> builder)
    {
        builder.ToTable("apu_resources", table =>
        {
            table.HasCheckConstraint("CK_apu_resources_consumption", "consumption > 0");
            table.HasCheckConstraint(
                "CK_apu_resources_waste",
                "waste_percentage >= 0 AND waste_percentage <= 100");
        });

        builder.HasKey(line => line.Id);

        builder.Property(line => line.Id)
            .HasColumnName("id");

        builder.Property(line => line.ApuId)
            .HasColumnName("apu_id")
            .IsRequired();

        builder.Property(line => line.ResourceId)
            .HasColumnName("resource_id")
            .IsRequired();

        builder.Property(line => line.Consumption)
            .HasColumnName("consumption")
            .HasPrecision(18, 10)
            .IsRequired();

        builder.Property(line => line.WastePercentage)
            .HasColumnName("waste_percentage")
            .HasPrecision(5, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.HasIndex(line => line.ApuId)
            .HasDatabaseName("IX_apu_resources_apu_id");

        builder.HasIndex(line => line.ResourceId)
            .HasDatabaseName("IX_apu_resources_resource_id");

        builder.HasOne<Apu>()
            .WithMany(apu => apu.Lines)
            .HasForeignKey(line => line.ApuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(line => line.Resource)
            .WithMany()
            .HasForeignKey(line => line.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
