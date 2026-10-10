using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public sealed class UserSignatureConfiguration : IEntityTypeConfiguration<UserSignature>
{
    public void Configure(EntityTypeBuilder<UserSignature> builder)
    {
        builder.ToTable("user_signatures");

        builder.HasKey(userSignature => userSignature.Id);

        builder.Property(userSignature => userSignature.Id)
            .HasColumnName("id");

        builder.Property(userSignature => userSignature.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(userSignature => userSignature.SignatureName)
            .HasColumnName("signature_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(userSignature => userSignature.SignatureImageBase64)
            .HasColumnName("signature_image_base64")
            .IsRequired();

        builder.Property(userSignature => userSignature.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(userSignature => userSignature.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(userSignature => userSignature.UserId)
            .IsUnique();
    }
}