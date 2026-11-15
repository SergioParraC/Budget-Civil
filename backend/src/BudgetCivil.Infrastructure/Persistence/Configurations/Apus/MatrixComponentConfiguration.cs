using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Apus;

public sealed class MatrixComponentConfiguration : IEntityTypeConfiguration<MatrixComponent>
{
    public void Configure(EntityTypeBuilder<MatrixComponent> builder)
    {
        builder.ToTable("matrix_components", table =>
        {
            table.HasCheckConstraint("CK_matrix_components_consumption", "consumption > 0");
            table.HasCheckConstraint(
                "CK_matrix_components_waste",
                "waste_percentage >= 0 AND waste_percentage <= 100");
        });

        builder.HasKey(component => component.Id);

        builder.Property(component => component.Id)
            .HasColumnName("id");

        builder.Property(component => component.MatrixResourceId)
            .HasColumnName("matrix_resource_id")
            .IsRequired();

        builder.Property(component => component.ComponentResourceId)
            .HasColumnName("component_resource_id")
            .IsRequired();

        builder.Property(component => component.Consumption)
            .HasColumnName("consumption")
            .HasPrecision(18, 10)
            .IsRequired();

        builder.Property(component => component.WastePercentage)
            .HasColumnName("waste_percentage")
            .HasPrecision(5, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.HasIndex(component => component.MatrixResourceId)
            .HasDatabaseName("IX_matrix_components_matrix_resource_id");

        builder.HasIndex(component => component.ComponentResourceId)
            .HasDatabaseName("IX_matrix_components_component_resource_id");

        builder.HasOne<Resource>()
            .WithMany()
            .HasForeignKey(component => component.MatrixResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(component => component.ComponentResource)
            .WithMany()
            .HasForeignKey(component => component.ComponentResourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
