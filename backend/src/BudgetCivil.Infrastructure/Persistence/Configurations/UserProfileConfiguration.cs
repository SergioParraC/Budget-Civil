using BudgetCivil.Domain.Users.Entities;
using BudgetCivil.Domain.Catalogs;
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

        builder.Property(userProfile => userProfile.CountryId)
            .HasColumnName("country_id");

        builder.Property(userProfile => userProfile.DepartmentId)
            .HasColumnName("department_id");

        builder.Property(userProfile => userProfile.MunicipalityId)
            .HasColumnName("municipality_id");

        builder.Property(userProfile => userProfile.ProfessionalCard)
            .HasColumnName("professional_card")
            .HasMaxLength(80);

        builder.Property(userProfile => userProfile.ProfessionalCardIssuedAt)
            .HasColumnName("professional_card_issued_at");

        builder.Property(userProfile => userProfile.ProfessionalCardFilePath)
            .HasColumnName("professional_card_file_path")
            .HasMaxLength(255);

        builder.HasIndex(userProfile => userProfile.DocumentNumber);

        builder.HasOne(userProfile => userProfile.Country)
            .WithMany()
            .HasForeignKey(userProfile => userProfile.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(userProfile => userProfile.Department)
            .WithMany()
            .HasForeignKey(userProfile => userProfile.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(userProfile => userProfile.Municipality)
            .WithMany()
            .HasForeignKey(userProfile => userProfile.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
