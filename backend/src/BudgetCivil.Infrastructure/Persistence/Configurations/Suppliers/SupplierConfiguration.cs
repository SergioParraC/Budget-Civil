using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Domain.Suppliers;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Suppliers;

/// <summary>
/// `suppliers`: directorio por compañía con ubicación opcional (`REQ-PROVEEDORES-01`,
/// `REQ-PROVEEDORES-03`, `D-27`, `D-96`). No hay índice único sobre razón social ni email
/// (`REQ-PROVEEDORES-06`).
/// </summary>
public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(supplier => supplier.Id);

        builder.Property(supplier => supplier.Id)
            .HasColumnName("id");

        builder.Property(supplier => supplier.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(supplier => supplier.BusinessName)
            .HasColumnName("business_name")
            .HasMaxLength(Supplier.BusinessNameMaxLength)
            .IsRequired();

        builder.Property(supplier => supplier.ContactName)
            .HasColumnName("contact_name")
            .HasMaxLength(Supplier.ContactNameMaxLength);

        builder.Property(supplier => supplier.ContactPhone)
            .HasColumnName("contact_phone")
            .HasMaxLength(Supplier.ContactPhoneMaxLength);

        builder.Property(supplier => supplier.Email)
            .HasColumnName("email")
            .HasMaxLength(Supplier.EmailMaxLength);

        builder.Property(supplier => supplier.Address)
            .HasColumnName("address")
            .HasMaxLength(Supplier.AddressMaxLength);

        builder.Property(supplier => supplier.Observations)
            .HasColumnName("observations")
            .HasMaxLength(Supplier.ObservationsMaxLength);

        builder.Property(supplier => supplier.CountryId)
            .HasColumnName("country_id");

        builder.Property(supplier => supplier.DepartmentId)
            .HasColumnName("department_id");

        builder.Property(supplier => supplier.MunicipalityId)
            .HasColumnName("municipality_id");

        builder.Property(supplier => supplier.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(supplier => supplier.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();

        builder.Property(supplier => supplier.CreatedByUserId)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(supplier => supplier.CreatedAtUtc)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(supplier => supplier.LastModifiedByUserId)
            .HasColumnName("last_modified_by")
            .IsRequired();

        builder.Property(supplier => supplier.LastModifiedAtUtc)
            .HasColumnName("last_modified_at")
            .IsRequired();

        // El directorio se filtra por compañía, razón social y ubicación (`REQ-PROVEEDORES-08`).
        builder.HasIndex(supplier => new { supplier.CompanyId, supplier.BusinessName })
            .HasDatabaseName("IX_suppliers_company_id_business_name");

        builder.HasIndex(supplier => new { supplier.CompanyId, supplier.CountryId })
            .HasDatabaseName("IX_suppliers_company_id_country_id");

        builder.HasIndex(supplier => new { supplier.CompanyId, supplier.DepartmentId })
            .HasDatabaseName("IX_suppliers_company_id_department_id");

        builder.HasIndex(supplier => new { supplier.CompanyId, supplier.MunicipalityId })
            .HasDatabaseName("IX_suppliers_company_id_municipality_id");

        builder.HasIndex(supplier => new { supplier.CompanyId, supplier.IsActive })
            .HasDatabaseName("IX_suppliers_company_id_is_active");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(supplier => supplier.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(supplier => supplier.Country)
            .WithMany()
            .HasForeignKey(supplier => supplier.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(supplier => supplier.Department)
            .WithMany()
            .HasForeignKey(supplier => supplier.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(supplier => supplier.Municipality)
            .WithMany()
            .HasForeignKey(supplier => supplier.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(supplier => supplier.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(supplier => supplier.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
