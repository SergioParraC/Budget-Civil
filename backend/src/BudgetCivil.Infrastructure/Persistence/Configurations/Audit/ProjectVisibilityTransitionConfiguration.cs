using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Audit;

public sealed class ProjectVisibilityTransitionConfiguration : IEntityTypeConfiguration<ProjectVisibilityTransition>
{
    public void Configure(EntityTypeBuilder<ProjectVisibilityTransition> builder)
    {
        builder.ToTable("project_visibility_transitions");

        builder.HasKey(transition => transition.Id);

        builder.Property(transition => transition.Id)
            .HasColumnName("id");

        builder.Property(transition => transition.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(transition => transition.PreviousIsPublic)
            .HasColumnName("previous_is_public")
            .IsRequired();

        builder.Property(transition => transition.NewIsPublic)
            .HasColumnName("new_is_public")
            .IsRequired();

        builder.Property(transition => transition.ChangedByUserId)
            .HasColumnName("changed_by_user_id")
            .IsRequired();

        builder.Property(transition => transition.ChangedAtUtc)
            .HasColumnName("changed_at_utc")
            .IsRequired();

        builder.Property(transition => transition.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500);

        builder.HasIndex(transition => new { transition.ProjectId, transition.ChangedAtUtc });

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(transition => transition.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(transition => transition.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
