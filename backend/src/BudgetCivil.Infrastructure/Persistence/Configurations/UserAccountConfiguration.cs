using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");

        builder.HasKey(userAccount => userAccount.Id);

        builder.Property(userAccount => userAccount.Id)
            .HasColumnName("id");

        builder.Property(userAccount => userAccount.CompanyId)
            .HasColumnName("company_id");

        builder.Property(userAccount => userAccount.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(userAccount => userAccount.Email)
            .HasColumnName("email")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(userAccount => userAccount.Username)
            .HasColumnName("username")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(userAccount => userAccount.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(userAccount => userAccount.AccountEnabled)
            .HasColumnName("account_enabled")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(userAccount => userAccount.AccountLocked)
            .HasColumnName("account_locked")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(userAccount => userAccount.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(userAccount => userAccount.LastLoginAtUtc)
            .HasColumnName("last_login_at_utc");

        builder.Property(userAccount => userAccount.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(userAccount => userAccount.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(userAccount => userAccount.Email)
            .IsUnique();

        builder.HasIndex(userAccount => userAccount.Username)
            .IsUnique();

        builder.HasOne(userAccount => userAccount.Company)
            .WithMany(company => company.UserAccounts)
            .HasForeignKey(userAccount => userAccount.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(userAccount => userAccount.Role)
            .WithMany(role => role.UserAccounts)
            .HasForeignKey(userAccount => userAccount.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(userAccount => userAccount.Profile)
            .WithOne(profile => profile.UserAccount)
            .HasForeignKey<UserProfile>(profile => profile.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
