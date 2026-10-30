using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Memberships;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");

        builder.HasKey(member => member.Id);

        builder.Property(member => member.Id)
            .HasColumnName("id");

        builder.Property(member => member.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(member => member.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(member => member.RoleInProject)
            .HasColumnName("role_in_project")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(member => member.AssignedByUserId)
            .HasColumnName("assigned_by_user_id")
            .IsRequired();

        builder.Property(member => member.AssignedAtUtc)
            .HasColumnName("assigned_at_utc")
            .IsRequired();

        // D-98: una sola pertenencia por (project_id, user_id), sin historial.
        builder.HasIndex(member => new { member.ProjectId, member.UserId })
            .IsUnique();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(member => member.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
