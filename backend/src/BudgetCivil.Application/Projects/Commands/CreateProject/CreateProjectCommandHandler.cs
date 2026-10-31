using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Projects.Commands.CreateProject;

public sealed class CreateProjectCommand
{
    public Guid? CompanyId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Code { get; init; }

    public string? Description { get; init; }

    public string? Objective { get; init; }

    public Guid? CountryId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? MunicipalityId { get; init; }

    public Guid? BaseCurrencyId { get; init; }

    public decimal? BudgetLimit { get; init; }

    public decimal? AiuAdminPercent { get; init; }

    public decimal? AiuUnforeseenPercent { get; init; }

    public decimal? AiuProfitPercent { get; init; }

    public string? SigningMode { get; init; }

    public Guid? SignerUserId { get; init; }

    public bool? RequiresAuditor { get; init; }
}

public sealed class CreateProjectResponse
{
    public Guid ProjectId { get; init; }

    public Guid CompanyId { get; init; }

    public string Status { get; init; } = string.Empty;

    public Guid CountryId { get; init; }

    public Guid DepartmentId { get; init; }

    public Guid MunicipalityId { get; init; }

    public Guid BaseCurrencyId { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Project.NameMaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(Project.DescriptionMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Description));

        RuleFor(command => command.Code)
            .MaximumLength(Project.CodeMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Code));

        RuleFor(command => command.Objective)
            .MaximumLength(Project.ObjectiveMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Objective));

        RuleFor(command => command.CountryId)
            .NotNull()
            .NotEqual(Guid.Empty);

        RuleFor(command => command.DepartmentId)
            .NotNull()
            .NotEqual(Guid.Empty);

        RuleFor(command => command.MunicipalityId)
            .NotNull()
            .NotEqual(Guid.Empty);

        RuleFor(command => command.BaseCurrencyId)
            .NotNull()
            .NotEqual(Guid.Empty);

        RuleFor(command => command.BudgetLimit)
            .GreaterThan(0m)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .When(command => command.BudgetLimit.HasValue);

        RuleFor(command => command.AiuAdminPercent)
            .InclusiveBetween(0m, 100m)
            .When(command => command.AiuAdminPercent.HasValue);

        RuleFor(command => command.AiuUnforeseenPercent)
            .InclusiveBetween(0m, 100m)
            .When(command => command.AiuUnforeseenPercent.HasValue);

        RuleFor(command => command.AiuProfitPercent)
            .InclusiveBetween(0m, 100m)
            .When(command => command.AiuProfitPercent.HasValue);

        RuleFor(command => command.SigningMode)
            .Must(signingMode =>
                string.IsNullOrWhiteSpace(signingMode) ||
                Project.IsValidSignatureMode(signingMode))
            .WithMessage("signing_mode is invalid.");
    }
}

public sealed class CreateProjectCommandHandler(
    IProjectRepository projectRepository,
    ISecurityAuditService securityAuditService,
    IValidator<CreateProjectCommand> validator)
{
    public async Task<CreateProjectResponse> ExecuteAsync(
        Guid actorUserId,
        CreateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);

        var actor = await projectRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = EnsureActorCanCreateOrUpdateProject(actor);

        var companyId = ResolveCompanyId(actor, command.CompanyId);
        var company = await projectRepository.GetCompanyByIdAsync(companyId, cancellationToken);
        if (company is null)
        {
            throw new ConflictAppException("Company was not found.", "COMPANY_NOT_FOUND");
        }

        if (!company.IsActive)
        {
            throw new ConflictAppException("Company is inactive.", "COMPANY_INACTIVE");
        }

        var countryId = command.CountryId!.Value;
        var departmentId = command.DepartmentId!.Value;
        var municipalityId = command.MunicipalityId!.Value;
        var baseCurrencyId = command.BaseCurrencyId!.Value;

        var referencesValid = await projectRepository.AreProjectCatalogReferencesValidAsync(
            countryId,
            departmentId,
            municipalityId,
            baseCurrencyId,
            cancellationToken);

        if (!referencesValid)
        {
            throw new ConflictAppException(
                "Project location or currency references are invalid.",
                "PROJECT_REFERENCE_INVALID");
        }

        var hasActiveDirector = await projectRepository.HasActiveDirectorInCompanyAsync(companyId, cancellationToken);
        if (!hasActiveDirector)
        {
            throw new ConflictAppException(
                "Project creation requires at least one active project manager in company.",
                "PROJECT_DIRECTOR_REQUIRED");
        }

        var directorOwnerUserId = actor.Role.Code == ProjectStatusTransitionPolicy.DirectorRoleCode
            ? actor.Id
            : await projectRepository.GetOldestActiveDirectorUserIdAsync(companyId, cancellationToken)
                ?? throw new ConflictAppException("Active project manager was not found.", "PROJECT_DIRECTOR_REQUIRED");

        var now = DateTime.UtcNow;
        Project project;
        try
        {
            project = new Project
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Code = Project.NormalizeCode(command.Code),
                Name = Project.NormalizeName(command.Name),
                Description = Project.NormalizeDescription(command.Description),
                Objective = Project.NormalizeObjective(command.Objective),
                CountryId = countryId,
                DepartmentId = departmentId,
                MunicipalityId = municipalityId,
                Status = ProjectStatus.PorCotizar,
                IsPublic = false,
                DirectorOwnerUserId = directorOwnerUserId,
                CreatedByUserId = actor.Id,
                LastModifiedByUserId = null,
                LastModifiedAtUtc = null,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Configuration = new ProjectConfiguration
                {
                    BaseCurrencyId = baseCurrencyId,
                    BudgetLimit = ProjectConfiguration.NormalizeBudgetLimit(command.BudgetLimit),
                    AiuAdminPercent = ProjectConfiguration.NormalizeAiuPercent(command.AiuAdminPercent, "aiu_admin_percent"),
                    AiuUnforeseenPercent = ProjectConfiguration.NormalizeAiuPercent(command.AiuUnforeseenPercent, "aiu_unforeseen_percent"),
                    AiuProfitPercent = ProjectConfiguration.NormalizeAiuPercent(command.AiuProfitPercent, "aiu_profit_percent"),
                    SignatureMode = Project.NormalizeSignatureModeOrDefault(command.SigningMode),
                    SignerUserId = command.SignerUserId,
                    RequiresAuditor = command.RequiresAuditor ?? false
                }
            };
        }
        catch (InvalidOperationException)
        {
            throw new ValidationAppException("Project payload contains invalid values.", "VALIDATION_ERROR");
        }

        project.Configuration.ProjectId = project.Id;

        await projectRepository.AddProjectAsync(project, cancellationToken);
        await projectRepository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                companyId,
                SecurityAuditEventType.ProjectAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "PROJECTS_CREATE",
                TargetType: nameof(Project),
                TargetId: project.Id,
                ReasonCode: "PROJECT_CREATED",
                Metadata: new Dictionary<string, string>
                {
                    ["actor_role_code"] = actor.Role.Code
                }),
            cancellationToken);

        return new CreateProjectResponse
        {
            ProjectId = project.Id,
            CompanyId = project.CompanyId,
            Status = ProjectStatusTransitionPolicy.ToStatusCode(project.Status),
            CountryId = project.CountryId,
            DepartmentId = project.DepartmentId,
            MunicipalityId = project.MunicipalityId,
            BaseCurrencyId = project.Configuration.BaseCurrencyId,
            CreatedAtUtc = project.CreatedAtUtc
        };
    }

    private static Guid ResolveCompanyId(UserAccount actor, Guid? requestedCompanyId)
    {
        if (actor.Role.Code == ProjectStatusTransitionPolicy.AdministratorRoleCode)
        {
            if (requestedCompanyId.HasValue)
            {
                return requestedCompanyId.Value;
            }

            if (actor.CompanyId.HasValue)
            {
                return actor.CompanyId.Value;
            }

            throw new ValidationAppException("company_id is required for admin project creation.", "VALIDATION_ERROR");
        }

        if (!actor.CompanyId.HasValue)
        {
            throw new ForbiddenAppException("Company context is required for this role.", "COMPANY_CONTEXT_REQUIRED");
        }

        return actor.CompanyId.Value;
    }

    private static UserAccount EnsureActorCanCreateOrUpdateProject(UserAccount? actor)
    {
        var resolvedActor = actor ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!resolvedActor.AccountEnabled || resolvedActor.AccountLocked || !resolvedActor.Role.IsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        if (resolvedActor.Role.Code is not ProjectStatusTransitionPolicy.AdministratorRoleCode and not ProjectStatusTransitionPolicy.DirectorRoleCode and not ProjectStatusTransitionPolicy.EstimatorRoleCode)
        {
            throw new ForbiddenAppException("Actor cannot create projects.", "PROJECT_CREATE_FORBIDDEN");
        }

        return resolvedActor;
    }

    private async Task ValidateAsync(CreateProjectCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (validationResult.IsValid)
        {
            return;
        }

        var message = string.Join(" | ", validationResult.Errors.Select(error => error.ErrorMessage));
        throw new ValidationAppException(message, "VALIDATION_ERROR");
    }
}
