using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(userProfile => userProfile.UserId);

        builder.Property(userProfile => userProfile.UserId)
            .HasColumnName("user_id");

        builder.Property(userProfile => userProfile.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(userProfile => userProfile.MiddleName)
            .HasColumnName("middle_name")
            .HasMaxLength(100);

        builder.Property(userProfile => userProfile.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(userProfile => userProfile.SecondLastName)
            .HasColumnName("second_last_name")
            .HasMaxLength(100);

        builder.Property(userProfile => userProfile.DocumentType)
            .HasColumnName("document_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(userProfile => userProfile.DocumentNumber)
            .HasColumnName("document_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(userProfile => userProfile.BirthDate)
            .HasColumnName("birth_date")
            .IsRequired();

        builder.Property(userProfile => userProfile.ResidenceCity)
            .HasColumnName("residence_city")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(userProfile => userProfile.ResidenceDepartment)
            .HasColumnName("residence_department")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(userProfile => userProfile.ResidenceCountry)
            .HasColumnName("residence_country")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(userProfile => userProfile.ProfessionalCard)
            .HasColumnName("professional_card")
            .HasMaxLength(80);

        builder.Property(userProfile => userProfile.ProfessionalCardIssuedAt)
            .HasColumnName("professional_card_issued_at");

        builder.Property(userProfile => userProfile.ProfessionalCardFilePath)
            .HasColumnName("professional_card_file_path")
            .HasMaxLength(255);

        builder.HasIndex(userProfile => userProfile.DocumentNumber);
    }
}
