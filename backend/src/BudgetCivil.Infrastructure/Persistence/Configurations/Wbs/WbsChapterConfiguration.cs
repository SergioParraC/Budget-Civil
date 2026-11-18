using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Domain.Wbs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Wbs;

public sealed class WbsChapterConfiguration : IEntityTypeConfiguration<WbsChapter>
{
    public void Configure(EntityTypeBuilder<WbsChapter> builder)
    {
        builder.ToTable("wbs_chapters", table =>
        {
            table.HasCheckConstraint("CK_wbs_chapters_order_index", "order_index >= 0");
        });

        builder.HasKey(chapter => chapter.Id);

        builder.Property(chapter => chapter.Id)
            .HasColumnName("id");

        builder.Property(chapter => chapter.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(chapter => chapter.ParentChapterId)
            .HasColumnName("parent_chapter_id");

        builder.Property(chapter => chapter.CodeId)
            .HasColumnName("code_id")
            .IsRequired();

        builder.Property(chapter => chapter.Name)
            .HasColumnName("name")
            .HasMaxLength(WbsChapter.NameMaxLength)
            .IsRequired();

        builder.Property(chapter => chapter.OrderIndex)
            .HasColumnName("order_index")
            .IsRequired();

        builder.Property(chapter => chapter.LastModifiedByUserId)
            .HasColumnName("last_modified_by")
            .IsRequired();

        builder.Property(chapter => chapter.LastModifiedAtUtc)
            .HasColumnName("last_modified_at")
            .IsRequired();

        builder.Property(chapter => chapter.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();

        builder.HasIndex(chapter => chapter.CodeId)
            .IsUnique()
            .HasDatabaseName("UQ_wbs_chapters_code_id");

        builder.HasIndex(chapter => new
            {
                chapter.ProjectId,
                chapter.ParentChapterId,
                chapter.OrderIndex
            })
            .HasDatabaseName("IX_wbs_chapters_project_id_parent_chapter_id_order_index");

        builder.HasOne(chapter => chapter.Code)
            .WithOne()
            .HasForeignKey<WbsChapter>(chapter => chapter.CodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(chapter => chapter.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WbsChapter>()
            .WithMany()
            .HasForeignKey(chapter => chapter.ParentChapterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(chapter => chapter.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
