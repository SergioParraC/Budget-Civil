using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public sealed class PasswordRecoveryTokenConfiguration : IEntityTypeConfiguration<PasswordRecoveryToken>
{
    public void Configure(EntityTypeBuilder<PasswordRecoveryToken> builder)
    {
        builder.ToTable("password_recovery_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id).HasColumnName("id");
        builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        builder.Property(token => token.ExpiresAtUtc).HasColumnName("expires_at_utc").IsRequired();
        builder.Property(token => token.ConsumedAtUtc).HasColumnName("consumed_at_utc");
        builder.Property(token => token.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder.HasOne(token => token.UserAccount)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}