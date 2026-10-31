using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations;

public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("users");

        builder.HasKey(userAccount => userAccount.Id);

        builder.Property(userAccount => userAccount.Id)
            .HasColumnName("id");

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

        builder.Property(userAccount => userAccount.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(userAccount => userAccount.MiddleName)
            .HasColumnName("middle_name")
            .HasMaxLength(100);

        builder.Property(userAccount => userAccount.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(userAccount => userAccount.SecondLastName)
            .HasColumnName("second_last_name")
            .HasMaxLength(100);

        builder.Property(userAccount => userAccount.DocumentType)
            .HasColumnName("document_type")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(userAccount => userAccount.DocumentNumber)
            .HasColumnName("document_number")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(userAccount => userAccount.BirthDate)
            .HasColumnName("birth_date")
            .IsRequired();

        builder.Property(userAccount => userAccount.CountryId)
            .HasColumnName("country_id");

        builder.Property(userAccount => userAccount.DepartmentId)
            .HasColumnName("department_id");

        builder.Property(userAccount => userAccount.MunicipalityId)
            .HasColumnName("municipality_id");

        builder.Property(userAccount => userAccount.ProfessionalCard)
            .HasColumnName("professional_card")
            .HasMaxLength(80);

        builder.Property(userAccount => userAccount.ProfessionalCardIssuedAt)
            .HasColumnName("professional_card_issued_at");

        builder.Property(userAccount => userAccount.ProfessionalCardFilePath)
            .HasColumnName("professional_card_file_path")
            .HasMaxLength(255);

        builder.HasIndex(userAccount => userAccount.Email)
            .IsUnique();

        builder.HasIndex(userAccount => userAccount.Username)
            .IsUnique();

        // D-77: el numero de documento no es unico; solo se indexa para busqueda.
        builder.HasIndex(userAccount => userAccount.DocumentNumber);

        builder.HasOne(userAccount => userAccount.Role)
            .WithMany(role => role.UserAccounts)
            .HasForeignKey(userAccount => userAccount.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(userAccount => userAccount.Country)
            .WithMany()
            .HasForeignKey(userAccount => userAccount.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(userAccount => userAccount.Department)
            .WithMany()
            .HasForeignKey(userAccount => userAccount.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(userAccount => userAccount.Municipality)
            .WithMany()
            .HasForeignKey(userAccount => userAccount.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);
        // `users_company` es la única fuente de verdad de la pertenencia. La membresía se autocarga
        // para que `UserAccount.CompanyId` (propiedad derivada) esté disponible sin consultas extra.
        builder.Navigation(userAccount => userAccount.Memberships).AutoInclude();


    }
}