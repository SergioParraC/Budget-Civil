using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Wbs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Wbs;

public sealed class WbsCodeConfiguration : IEntityTypeConfiguration<WbsCode>
{
    public void Configure(EntityTypeBuilder<WbsCode> builder)
    {
        builder.ToTable("wbs_codes", table =>
        {
            table.HasCheckConstraint("CK_wbs_codes_code_length", "LEN(code) BETWEEN 1 AND 10");
            table.HasCheckConstraint("CK_wbs_codes_node_type", "node_type IN ('CHAPTER','ITEM')");
        });

        builder.HasKey(code => code.Id);

        builder.Property(code => code.Id)
            .HasColumnName("id");

        builder.Property(code => code.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        // `BR-015`: unicidad sin distinguir mayúsculas declarada a nivel de columna.
        builder.Property(code => code.Code)
            .HasColumnName("code")
            .HasColumnType("varchar(10)")
            .UseCollation("Latin1_General_100_CI_AS")
            .IsRequired();

        builder.Property(code => code.NodeType)
            .HasColumnName("node_type")
            .HasConversion(
                nodeType => WbsNodeTypeCodes.ToCode(nodeType),
                code => WbsNodeTypeCodes.Parse(code))
            .HasColumnType("varchar(10)")
            .IsRequired();

        builder.HasIndex(code => new { code.ProjectId, code.Code })
            .IsUnique()
            .HasDatabaseName("UQ_wbs_codes_project_id_code");

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(code => code.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
