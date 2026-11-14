using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Domain.Wbs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Wbs;

public sealed class WbsItemConfiguration : IEntityTypeConfiguration<WbsItem>
{
    public void Configure(EntityTypeBuilder<WbsItem> builder)
    {
        builder.ToTable("wbs_items", table =>
        {
            table.HasCheckConstraint("CK_wbs_items_quantity_work", "quantity_work > 0");
            table.HasCheckConstraint("CK_wbs_items_order_index", "order_index >= 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id");

        builder.Property(item => item.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        // `D-118`: nulo = ítem raíz del presupuesto.
        builder.Property(item => item.WbsChapterId)
            .HasColumnName("wbs_chapter_id");

        // `D-113`: sin FK hasta `I-3b`; la unicidad 1:1 se garantiza con el índice filtrado.
        builder.Property(item => item.ApuId)
            .HasColumnName("apu_id");

        builder.Property(item => item.CodeId)
            .HasColumnName("code_id")
            .IsRequired();

        builder.Property(item => item.QuantityWork)
            .HasColumnName("quantity_work")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(item => item.OrderIndex)
            .HasColumnName("order_index")
            .IsRequired();

        builder.Property(item => item.LastModifiedByUserId)
            .HasColumnName("last_modified_by")
            .IsRequired();

        builder.Property(item => item.LastModifiedAtUtc)
            .HasColumnName("last_modified_at")
            .IsRequired();

        builder.Property(item => item.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();

        builder.HasIndex(item => item.CodeId)
            .IsUnique()
            .HasDatabaseName("UQ_wbs_items_code_id");

        builder.HasIndex(item => item.ApuId)
            .IsUnique()
            .HasFilter("[apu_id] IS NOT NULL")
            .HasDatabaseName("UQ_wbs_items_apu_id");

        builder.HasIndex(item => new
            {
                item.ProjectId,
                item.WbsChapterId,
                item.OrderIndex
            })
            .HasDatabaseName("IX_wbs_items_project_id_wbs_chapter_id_order_index");

        builder.HasOne(item => item.Code)
            .WithOne()
            .HasForeignKey<WbsItem>(item => item.CodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(item => item.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WbsChapter>()
            .WithMany()
            .HasForeignKey(item => item.WbsChapterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(item => item.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
