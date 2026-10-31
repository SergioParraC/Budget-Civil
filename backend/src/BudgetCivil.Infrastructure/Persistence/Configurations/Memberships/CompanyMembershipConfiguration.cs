using BudgetCivil.Domain.Memberships;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Memberships;

/// <summary>
/// Mapeo de la tabla <c>users_company</c>: historial de vínculos usuario-compañía y única fuente de
/// verdad de la pertenencia vigente (<c>account_enable = 1</c>).
/// </summary>
public sealed class CompanyMembershipConfiguration : IEntityTypeConfiguration<CompanyMembership>
{
    public void Configure(EntityTypeBuilder<CompanyMembership> builder)
    {
        builder.ToTable("users_company");

        builder.HasKey(membership => membership.Id);

        builder.Property(membership => membership.Id)
            .HasColumnName("id");

        builder.Property(membership => membership.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(membership => membership.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(membership => membership.AccountEnabled)
            .HasColumnName("account_enable")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(membership => membership.CreatedAtUtc)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(membership => membership.UpdatedAtUtc)
            .HasColumnName("updated_at")
            .IsRequired();

        // Un usuario solo puede tener un vínculo vigente; el historial queda en las filas cerradas.
        builder.HasIndex(membership => membership.UserId)
            .IsUnique()
            .HasFilter("[account_enable] = 1");

        builder.HasIndex(membership => new { membership.CompanyId, membership.AccountEnabled });

        builder.HasOne<UserAccount>()
            .WithMany(userAccount => userAccount.Memberships)
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(membership => membership.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
