using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Memberships;

public sealed class ProjectUserMembershipConfiguration : IEntityTypeConfiguration<ProjectUserMembership>
{
    public void Configure(EntityTypeBuilder<ProjectUserMembership> builder)
    {
        builder.ToTable("project_user_memberships");

        builder.HasKey(membership => membership.Id);

        builder.Property(membership => membership.Id)
            .HasColumnName("id");

        builder.Property(membership => membership.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(membership => membership.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(membership => membership.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(membership => membership.AssignedByUserId)
            .HasColumnName("assigned_by_user_id")
            .IsRequired();

        builder.Property(membership => membership.AssignedAtUtc)
            .HasColumnName("assigned_at_utc")
            .IsRequired();

        builder.Property(membership => membership.EndedAtUtc)
            .HasColumnName("ended_at_utc");

        builder.HasIndex(membership => new { membership.ProjectId, membership.UserId })
            .HasFilter("[is_active] = 1")
            .IsUnique();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(membership => membership.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(membership => membership.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
