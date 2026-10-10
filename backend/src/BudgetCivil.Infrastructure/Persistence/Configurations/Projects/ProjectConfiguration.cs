using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Projects;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Id)
            .HasColumnName("id");

        builder.Property(project => project.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(project => project.Code)
            .HasColumnName("code")
            .HasMaxLength(50);

        builder.Property(project => project.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(project => project.Objective)
            .HasColumnName("objective")
            .HasMaxLength(1000);

        builder.Property(project => project.CountryId)
            .HasColumnName("country_id")
            .IsRequired();

        builder.Property(project => project.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(project => project.MunicipalityId)
            .HasColumnName("municipality_id")
            .IsRequired();

        builder.Property(project => project.BaseCurrencyId)
            .HasColumnName("base_currency_id")
            .IsRequired();

        builder.Property(project => project.AiuAdminPercent)
            .HasColumnName("aiu_admin_percent")
            .HasPrecision(5, 2);

        builder.Property(project => project.AiuUnforeseenPercent)
            .HasColumnName("aiu_unforeseen_percent")
            .HasPrecision(5, 2);

        builder.Property(project => project.AiuProfitPercent)
            .HasColumnName("aiu_profit_percent")
            .HasPrecision(5, 2);

        builder.Property(project => project.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(project => project.IsPublic)
            .HasColumnName("is_public")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(project => project.SigningMode)
            .HasColumnName("signing_mode")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(project => project.SignerUserId)
            .HasColumnName("signer_user_id");

        builder.Property(project => project.DirectorOwnerUserId)
            .HasColumnName("director_owner_user_id")
            .IsRequired();

        builder.Property(project => project.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(project => project.LastModifiedByUserId)
            .HasColumnName("last_modified_by_user_id");

        builder.Property(project => project.LastModifiedAtUtc)
            .HasColumnName("last_modified_at_utc");

        builder.Property(project => project.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(project => project.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(project => new { project.CompanyId, project.Status });

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(project => project.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Country>()
            .WithMany()
            .HasForeignKey(project => project.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(project => project.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Municipality>()
            .WithMany()
            .HasForeignKey(project => project.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BaseCurrency>()
            .WithMany()
            .HasForeignKey(project => project.BaseCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(project => project.DirectorOwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(project => project.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(project => project.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(project => project.SignerUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
