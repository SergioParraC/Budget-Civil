using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Domain.Catalogs;
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

        builder.Property(company => company.CompanyType)
            .HasColumnName("company_type")
            .HasMaxLength(20);

        builder.Property(company => company.TerritorialLevel)
            .HasColumnName("territorial_level")
            .HasMaxLength(50);

        builder.Property(company => company.CountryId)
            .HasColumnName("country_id");

        builder.Property(company => company.DepartmentId)
            .HasColumnName("department_id");

        builder.Property(company => company.MunicipalityId)
            .HasColumnName("municipality_id");

        builder.Property(company => company.RepresentativeMode)
            .HasColumnName("representative_mode")
            .HasMaxLength(20);

        builder.Property(company => company.RepresentativeUserId)
            .HasColumnName("representative_user_id");

        builder.Property(company => company.RepresentativeFullName)
            .HasColumnName("representative_full_name")
            .HasMaxLength(200);

        builder.Property(company => company.RepresentativeDocumentTypeId)
            .HasColumnName("representative_document_type_id");

        builder.Property(company => company.RepresentativeDocumentNumber)
            .HasColumnName("representative_document_number")
            .HasMaxLength(50);

        builder.Property(company => company.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(company => company.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(company => company.CreatedAtUtc)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(company => company.LastModifiedByUserId)
            .HasColumnName("last_modified_by_user_id");

        builder.Property(company => company.LastModifiedAtUtc)
            .HasColumnName("last_modified_at");

        builder.HasIndex(company => company.TaxId)
            .IsUnique();

        builder.HasOne(company => company.Country)
            .WithMany()
            .HasForeignKey(company => company.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(company => company.Department)
            .WithMany()
            .HasForeignKey(company => company.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(company => company.Municipality)
            .WithMany()
            .HasForeignKey(company => company.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DocumentType>()
            .WithMany()
            .HasForeignKey(company => company.RepresentativeDocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(company => company.RepresentativeUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(company => company.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(company => company.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
