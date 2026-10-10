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
    [JsonPropertyName("company_id")]
    public Guid? CompanyId { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("objective")]
    public string? Objective { get; init; }

    [JsonPropertyName("country_id")]
    public Guid? CountryId { get; init; }

    [JsonPropertyName("department_id")]
    public Guid? DepartmentId { get; init; }

    [JsonPropertyName("municipality_id")]
    public Guid? MunicipalityId { get; init; }

    [JsonPropertyName("base_currency_id")]
    public Guid? BaseCurrencyId { get; init; }

    [JsonPropertyName("aiu_admin_percent")]
    public decimal? AiuAdminPercent { get; init; }

    [JsonPropertyName("aiu_unforeseen_percent")]
    public decimal? AiuUnforeseenPercent { get; init; }

    [JsonPropertyName("aiu_profit_percent")]
    public decimal? AiuProfitPercent { get; init; }

    [JsonPropertyName("signing_mode")]
    public string? SigningMode { get; init; }

    [JsonPropertyName("signer_user_id")]
    public Guid? SignerUserId { get; init; }
}

public sealed class CreateProjectResponse
{
    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; init; }

    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("country_id")]
    public Guid CountryId { get; init; }

    [JsonPropertyName("department_id")]
    public Guid DepartmentId { get; init; }

    [JsonPropertyName("municipality_id")]
    public Guid MunicipalityId { get; init; }

    [JsonPropertyName("base_currency_id")]
    public Guid BaseCurrencyId { get; init; }

    [JsonPropertyName("created_at_utc")]
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
                string.Equals(signingMode, Project.UniqueSignerByAssignedEstimator, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(signingMode, Project.IndependentSignerByResponsibleProfessional, StringComparison.OrdinalIgnoreCase))
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

        var directorOwnerUserId = actor.Role.Code == ProjectStatusTransitionPolicy.ProjectManagerRoleCode
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
                BaseCurrencyId = baseCurrencyId,
                AiuAdminPercent = Project.NormalizeAiuPercent(command.AiuAdminPercent, "aiu_admin_percent"),
                AiuUnforeseenPercent = Project.NormalizeAiuPercent(command.AiuUnforeseenPercent, "aiu_unforeseen_percent"),
                AiuProfitPercent = Project.NormalizeAiuPercent(command.AiuProfitPercent, "aiu_profit_percent"),
                Status = ProjectStatus.PorCotizar,
                IsPublic = false,
                SigningMode = Project.NormalizeSigningModeOrDefault(command.SigningMode),
                SignerUserId = command.SignerUserId,
                DirectorOwnerUserId = directorOwnerUserId,
                CreatedByUserId = actor.Id,
                LastModifiedByUserId = null,
                LastModifiedAtUtc = null,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
        }
        catch (InvalidOperationException)
        {
            throw new ValidationAppException("Project payload contains invalid values.", "VALIDATION_ERROR");
        }

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
            BaseCurrencyId = project.BaseCurrencyId,
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

        if (resolvedActor.Role.Code is not ProjectStatusTransitionPolicy.AdministratorRoleCode and not ProjectStatusTransitionPolicy.ProjectManagerRoleCode and not ProjectStatusTransitionPolicy.EstimatorRoleCode)
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
