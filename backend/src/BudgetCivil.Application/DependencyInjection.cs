using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Catalogs.Queries;
using BudgetCivil.Application.Companies.ChangeCompanyStatus;
using BudgetCivil.Application.Companies.CreateCompany;
using BudgetCivil.Application.Companies.ListCompanies;
using BudgetCivil.Application.Companies.UseCases;
using BudgetCivil.Application.Companies.UpdateCompany;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Memberships.Companies;
using BudgetCivil.Application.Memberships.Projects;
using BudgetCivil.Application.Projects.Commands.ChangeProjectStatus;
using BudgetCivil.Application.Projects.Commands.ChangeProjectVisibility;
using BudgetCivil.Application.Projects.Commands.CreateProject;
using BudgetCivil.Application.Projects.Commands.UpdateProject;
using BudgetCivil.Application.Projects.Queries;
using BudgetCivil.Application.Profile.UseCases;
using BudgetCivil.Application.Users.Administration.Guards;
using BudgetCivil.Application.Users.Administration.UseCases;
using BudgetCivil.Application.Users.Auth.UseCases;
using BudgetCivil.Application.Users.Bootstrap.UseCases;
using BudgetCivil.Application.Users.Search.UseCases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetCivil.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<ICompanyContextAccessor, CompanyContextAccessor>();
        services.AddScoped<RejectProjectAssignmentGuard>();
        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<LoginUseCase>();
        services.AddScoped<BootstrapFirstAdministratorUseCase>();
        services.AddScoped<GetBootstrapAuditUseCase>();
        services.AddScoped<CreateUserUseCase>();
        services.AddScoped<UpdateUserUseCase>();
        services.AddScoped<DeactivateUserUseCase>();
        services.AddScoped<ChangeUserRoleUseCase>();
        services.AddScoped<ChangeUserCompanyUseCase>();
        services.AddScoped<CreateCompanyUseCase>();
        services.AddScoped<UpdateCompanyUseCase>();
        services.AddScoped<DeactivateCompanyUseCase>();
        services.AddScoped<CreateCompanyCommandHandler>();
        services.AddScoped<UpdateCompanyCommandHandler>();
        services.AddScoped<DisableCompanyCommandHandler>();
        services.AddScoped<EnableCompanyCommandHandler>();
        services.AddScoped<GetCompaniesQueryHandler>();
        services.AddScoped<ListingQueryPipeline>();
        services.AddScoped<GetCatalogItemsQueryHandler>();
        services.AddScoped<GetScopedUsersQueryHandler>();
        services.AddScoped<GetScopedProjectsQueryHandler>();
        services.AddScoped<CreateProjectCommandHandler>();
        services.AddScoped<UpdateProjectCommandHandler>();
        services.AddScoped<ChangeProjectStatusCommandHandler>();
        services.AddScoped<ChangeProjectVisibilityCommandHandler>();
        services.AddScoped<AssignCompanyMemberCommandHandler>();
        services.AddScoped<DeactivateCompanyMemberCommandHandler>();
        services.AddScoped<AssignProjectMemberCommandHandler>();
        services.AddScoped<DeactivateProjectMemberCommandHandler>();
        services.AddScoped<GetProjectsQueryHandler>();
        services.AddScoped<GetProjectByIdQueryHandler>();
        services.AddScoped<GetMyProfileUseCase>();
        services.AddScoped<UpdateMyProfileUseCase>();
        services.AddScoped<ChangeMyPasswordUseCase>();
        services.AddScoped<EnableUserPasswordRecoveryUseCase>();
        services.AddScoped<SearchUsersUseCase>();

        return services;
    }
}
