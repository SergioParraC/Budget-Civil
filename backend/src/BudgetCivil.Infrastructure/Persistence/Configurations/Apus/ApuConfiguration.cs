using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Apus;

public sealed class ApuConfiguration : IEntityTypeConfiguration<Apu>
{
    public void Configure(EntityTypeBuilder<Apu> builder)
    {
        builder.ToTable("apus", table =>
        {
            table.HasCheckConstraint(
                "CK_apus_status",
                "status IN ('COMPLETO','FALTA_INFORMACION')");
        });

        builder.HasKey(apu => apu.Id);

        builder.Property(apu => apu.Id)
            .HasColumnName("id");

        builder.Property(apu => apu.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(apu => apu.Description)
            .HasColumnName("description")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(apu => apu.Unit)
            .HasColumnName("unit")
            .HasMaxLength(Apu.UnitMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(apu => apu.Status)
            .HasColumnName("status")
            .HasConversion(
                status => ApuStatusCodes.ToCode(status),
                code => ApuStatusCodes.Parse(code))
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(apu => apu.LastModifiedByUserId)
            .HasColumnName("last_modified_by")
            .IsRequired();

        builder.Property(apu => apu.LastModifiedAtUtc)
            .HasColumnName("last_modified_at")
            .IsRequired();

        builder.Property(apu => apu.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion();

        builder.HasIndex(apu => new { apu.ProjectId, apu.Status })
            .HasDatabaseName("IX_apus_project_id_status");

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(apu => apu.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(apu => apu.LastModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
