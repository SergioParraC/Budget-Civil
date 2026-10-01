using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Audit;

public sealed class ProjectStatusTransitionConfiguration : IEntityTypeConfiguration<ProjectStatusTransition>
{
    public void Configure(EntityTypeBuilder<ProjectStatusTransition> builder)
    {
        builder.ToTable("project_status_transitions");

        builder.HasKey(transition => transition.Id);

        builder.Property(transition => transition.Id)
            .HasColumnName("id");

        builder.Property(transition => transition.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(transition => transition.PreviousStatus)
            .HasColumnName("previous_status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(transition => transition.NewStatus)
            .HasColumnName("new_status")
            .HasConversion<string>()
            .HasMaxLength(50)
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

        builder.Property(transition => transition.TransitionKind)
            .HasColumnName("transition_kind")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

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
