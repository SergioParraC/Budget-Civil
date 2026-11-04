using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Resources;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Resources;

public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("resources", table =>
        {
            table.HasCheckConstraint(
                "CK_resources_quotation_status",
                "quotation_status IN ('FALTA_INFORMACION','POR_COTIZAR','COTIZADO')");
            table.HasCheckConstraint(
                "CK_resources_unit_price",
                "unit_price IS NULL OR unit_price >= 0");
        });

        builder.HasKey(resource => resource.Id);

        builder.Property(resource => resource.Id)
            .HasColumnName("id");

        builder.Property(resource => resource.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(resource => resource.ResourceTypeId)
            .HasColumnName("resource_type_id")
            .IsRequired();

        builder.Property(resource => resource.MunicipalityId)
            .HasColumnName("municipality_id")
            .IsRequired();

        builder.Property(resource => resource.Description)
            .HasColumnName("description")
            .HasMaxLength(Resource.DescriptionMaxLength)
            .IsRequired();

        builder.Property(resource => resource.Unit)
            .HasColumnName("unit")
            .HasMaxLength(Resource.UnitMaxLength);

        builder.Property(resource => resource.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(18, 2);

        builder.Property(resource => resource.Brand)
            .HasColumnName("brand")
            .HasMaxLength(Resource.BrandMaxLength);

        builder.Property(resource => resource.Distributor)
            .HasColumnName("distributor")
            .HasMaxLength(Resource.DistributorMaxLength);

        builder.Property(resource => resource.UserKey)
            .HasColumnName("user_key")
            .HasMaxLength(Resource.UserKeyMaxLength);

        builder.Property(resource => resource.Details)
            .HasColumnName("details")
            .HasColumnType("nvarchar(max)");

        builder.Property(resource => resource.ContactNumber)
            .HasColumnName("contact_number")
            .HasMaxLength(Resource.ContactNumberMaxLength);

        builder.Property(resource => resource.QuotationStatus)
            .HasColumnName("quotation_status")
            .HasConversion(
                status => QuotationStatusCodes.ToCode(status),
                code => QuotationStatusCodes.Parse(code))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(resource => resource.OriginResourceId)
            .HasColumnName("origin_resource_id");

        builder.Property(resource => resource.LastModifiedByUserId)
            .HasColumnName("last_modified_by")
            .IsRequired();

        builder.Property(resource => resource.LastModifiedAtUtc)
            .HasColumnName("last_modified_at")
            .IsRequired();

        builder.Property(resource => resource.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();

        builder.Property(resource => resource.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(resource => resource.DeletedAtUtc)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(resource => resource.IsActive);

        builder.HasIndex(resource => new { resource.ProjectId, resource.ResourceTypeId })
            .HasDatabaseName("IX_resources_project_id_resource_type_id");

        builder.HasIndex(resource => new { resource.ProjectId, resource.QuotationStatus })
            .HasDatabaseName("IX_resources_project_id_quotation_status");

        builder.HasIndex(resource => new { resource.ProjectId, resource.Description })
            .HasDatabaseName("IX_resources_project_id_description");

        builder.HasIndex(resource => resource.OriginResourceId)
            .HasDatabaseName("IX_resources_origin_resource_id");

        builder.HasOne(resource => resource.Project)
            .WithMany()
            .HasForeignKey(resource => resource.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(resource => resource.ResourceType)
            .WithMany()
            .HasForeignKey(resource => resource.ResourceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(resource => resource.Municipality)
            .WithMany()
            .HasForeignKey(resource => resource.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(resource => resource.OriginResource)
            .WithMany()
            .HasForeignKey(resource => resource.OriginResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(resource => resource.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
