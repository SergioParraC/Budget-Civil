using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Domain.Wbs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Wbs;

public sealed class WbsProjectActivityConfiguration : IEntityTypeConfiguration<WbsProjectActivity>
{
    public void Configure(EntityTypeBuilder<WbsProjectActivity> builder)
    {
        builder.ToTable("wbs_project_activity");

        // `D-120`: una fila por proyecto y usuario, sin historial.
        builder.HasKey(activity => new { activity.ProjectId, activity.UserId });

        builder.Property(activity => activity.ProjectId)
            .HasColumnName("project_id");

        builder.Property(activity => activity.UserId)
            .HasColumnName("user_id");

        builder.Property(activity => activity.LastModifiedAtUtc)
            .HasColumnName("last_modified_at")
            .IsRequired();

        builder.HasIndex(activity => new { activity.ProjectId, activity.LastModifiedAtUtc })
            .HasDatabaseName("IX_wbs_project_activity_project_id_last_modified_at");

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(activity => activity.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(activity => activity.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
