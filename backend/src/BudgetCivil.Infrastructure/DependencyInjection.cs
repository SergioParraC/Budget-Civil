using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Companies.Repositories;
using BudgetCivil.Application.Listings.Repositories;
using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Application.Profile.Repositories;
using BudgetCivil.Application.Users.Administration.Repositories;
using BudgetCivil.Application.Users.Auth.Repositories;
using BudgetCivil.Application.Users.Bootstrap.Repositories;
using BudgetCivil.Application.Users.Search.Repositories;
using BudgetCivil.Infrastructure.Persistence;
using BudgetCivil.Infrastructure.Persistence.Repositories;
using BudgetCivil.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BudgetCivilDatabase")
            ?? throw new InvalidOperationException("Connection string 'BudgetCivilDatabase' is not configured.");

        services.AddDbContext<BudgetCivilDbContext>(options => options.UseSqlServer(connectionString));

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ISecurityAuditRepository, SecurityAuditRepository>();
        services.AddScoped<IGeographicCatalogRepository, GeographicCatalogRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IBootstrapRepository, BootstrapRepository>();
        services.AddScoped<IUserAdministrationRepository, UserAdministrationRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IListingRepository, ListingRepository>();
        services.AddScoped<ICompanyMembershipRepository, CompanyMembershipRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectMembershipRepository, ProjectMembershipRepository>();
        services.AddScoped<IProjectTransitionRepository, ProjectTransitionRepository>();
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IUserSearchRepository, UserSearchRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
        services.AddScoped<ISecurityBaselineSeeder, SecurityBaselineSeeder>();

        return services;
    }
}
