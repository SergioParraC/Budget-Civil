using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BudgetCivil.Infrastructure.Persistence.Configurations.Indexes;

public sealed class CompanyListingIndexesConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.HasIndex(company => new { company.IsActive, company.BusinessName });
    }
}

public sealed class UserAccountListingIndexesConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.HasIndex(user => new { user.CompanyId, user.RoleId, user.AccountEnabled });
    }
}

public sealed class ProjectListingIndexesConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasIndex(project => new { project.CompanyId, project.Name });
        builder.HasIndex(project => new { project.Status, project.IsPublic });
    }
}
