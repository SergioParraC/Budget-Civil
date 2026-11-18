using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Projects;

public sealed class ProjectConfigurationEntityConfiguration : IEntityTypeConfiguration<ProjectConfiguration>
{
    public void Configure(EntityTypeBuilder<ProjectConfiguration> builder)
    {
        builder.Property(configuration => configuration.RowVersion).IsRowVersion();

        builder.ToTable("project_configurations", table =>
        {
            table.HasCheckConstraint(
                "CK_project_configurations_rounding_decimals",
                "rounding_decimals >= 0 AND rounding_decimals <= 10");
            table.HasCheckConstraint(
                "CK_project_configurations_quantity_decimals",
                "quantity_decimals >= 0 AND quantity_decimals <= 10");
        });

        // La clave es el propio proyecto: garantiza una sola configuración por proyecto (SC-PROYECTOS-23).
        builder.HasKey(configuration => configuration.ProjectId);

        builder.Property(configuration => configuration.ProjectId)
            .HasColumnName("project_id")
            .ValueGeneratedNever();

        builder.Property(configuration => configuration.BaseCurrencyId)
            .HasColumnName("base_currency_id")
            .IsRequired();

        builder.Property(configuration => configuration.BudgetLimit)
            .HasColumnName("budget_limit")
            .HasPrecision(18, 2);

        builder.Property(configuration => configuration.AiuAdminPercent)
            .HasColumnName("aiu_admin_percent")
            .HasPrecision(5, 2);

        builder.Property(configuration => configuration.AiuUnforeseenPercent)
            .HasColumnName("aiu_unforeseen_percent")
            .HasPrecision(5, 2);

        builder.Property(configuration => configuration.AiuProfitPercent)
            .HasColumnName("aiu_profit_percent")
            .HasPrecision(5, 2);

        builder.Property(configuration => configuration.RoundingDecimals)
            .HasColumnName("rounding_decimals")
            .HasDefaultValue(ProjectConfiguration.DefaultDecimals)
            .IsRequired();

        builder.Property(configuration => configuration.QuantityDecimals)
            .HasColumnName("quantity_decimals")
            .HasDefaultValue(ProjectConfiguration.DefaultDecimals)
            .IsRequired();

        builder.Property(configuration => configuration.SignatureMode)
            .HasColumnName("signature_mode")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(configuration => configuration.SignerUserId)
            .HasColumnName("signer_user_id");

        builder.Property(configuration => configuration.RequiresAuditor)
            .HasColumnName("requires_auditor")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasOne<BaseCurrency>()
            .WithMany()
            .HasForeignKey(configuration => configuration.BaseCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(configuration => configuration.SignerUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
