using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public sealed class UserCompanyMembershipConfiguration : IEntityTypeConfiguration<UserCompanyMembership>
{
    public void Configure(EntityTypeBuilder<UserCompanyMembership> builder)
    {
        builder.ToTable("user_company_memberships");

        builder.HasKey(membership => membership.Id);

        builder.Property(membership => membership.Id)
            .HasColumnName("id");

        builder.Property(membership => membership.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(membership => membership.CompanyId)
            .HasColumnName("company_id")
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

        builder.HasIndex(membership => membership.UserId)
            .HasFilter("[is_active] = 1")
            .IsUnique();

        builder.HasOne(membership => membership.UserAccount)
            .WithMany(user => user.Memberships)
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(membership => membership.Company)
            .WithMany(company => company.Memberships)
            .HasForeignKey(membership => membership.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(membership => membership.AssignedByUser)
            .WithMany()
            .HasForeignKey(membership => membership.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
