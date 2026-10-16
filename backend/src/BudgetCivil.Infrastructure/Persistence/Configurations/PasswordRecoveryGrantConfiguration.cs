using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public sealed class PasswordRecoveryGrantConfiguration : IEntityTypeConfiguration<PasswordRecoveryGrant>
{
    public void Configure(EntityTypeBuilder<PasswordRecoveryGrant> builder)
    {
        builder.ToTable("password_recovery_grants");

        builder.HasKey(grant => grant.Id);

        builder.Property(grant => grant.Id)
            .HasColumnName("id");

        builder.Property(grant => grant.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(grant => grant.EnabledByUserId)
            .HasColumnName("enabled_by_user_id")
            .IsRequired();

        builder.Property(grant => grant.Reason)
            .HasColumnName("reason")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(grant => grant.EnabledAtUtc)
            .HasColumnName("enabled_at_utc")
            .IsRequired();

        builder.Property(grant => grant.ExpiresAtUtc)
            .HasColumnName("expires_at_utc");

        builder.Property(grant => grant.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(grant => grant.ConsumedAtUtc)
            .HasColumnName("consumed_at_utc");

        builder.HasOne(grant => grant.UserAccount)
            .WithMany(user => user.RecoveryGrants)
            .HasForeignKey(grant => grant.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(grant => grant.EnabledByUser)
            .WithMany(user => user.EnabledRecoveryGrants)
            .HasForeignKey(grant => grant.EnabledByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
