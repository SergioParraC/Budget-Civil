using System.Text.Json.Serialization;
using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;
using FluentValidation;

namespace BudgetCivil.Application.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommand
{
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

public sealed class UpdateProjectResponse
{
    [JsonPropertyName("project_id")]
    public Guid ProjectId { get; init; }

    [JsonPropertyName("updated_at_utc")]
    public DateTime UpdatedAtUtc { get; init; }
}

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(Project.NameMaxLength);

        RuleFor(command => command.Code)
            .MaximumLength(Project.CodeMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Code));

        RuleFor(command => command.Description)
            .MaximumLength(Project.DescriptionMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Description));

        RuleFor(command => command.Objective)
            .MaximumLength(Project.ObjectiveMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Objective));

        RuleFor(command => command.SigningMode)
            .Must(signingMode =>
                string.IsNullOrWhiteSpace(signingMode) ||
                string.Equals(signingMode, Project.UniqueSignerByAssignedEstimator, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(signingMode, Project.IndependentSignerByResponsibleProfessional, StringComparison.OrdinalIgnoreCase))
            .WithMessage("signing_mode is invalid.");

        RuleFor(command => command.CountryId)
            .NotEqual(Guid.Empty)
            .When(command => command.CountryId.HasValue);

        RuleFor(command => command.DepartmentId)
            .NotEqual(Guid.Empty)
            .When(command => command.DepartmentId.HasValue);

        RuleFor(command => command.MunicipalityId)
            .NotEqual(Guid.Empty)
            .When(command => command.MunicipalityId.HasValue);

        RuleFor(command => command.BaseCurrencyId)
            .NotEqual(Guid.Empty)
            .When(command => command.BaseCurrencyId.HasValue);

        RuleFor(command => command.AiuAdminPercent)
            .InclusiveBetween(0m, 100m)
            .When(command => command.AiuAdminPercent.HasValue);

        RuleFor(command => command.AiuUnforeseenPercent)
            .InclusiveBetween(0m, 100m)
            .When(command => command.AiuUnforeseenPercent.HasValue);

        RuleFor(command => command.AiuProfitPercent)
            .InclusiveBetween(0m, 100m)
            .When(command => command.AiuProfitPercent.HasValue);
    }
}

public sealed class UpdateProjectCommandHandler(
    IProjectRepository projectRepository,
    ISecurityAuditService securityAuditService,
    IValidator<UpdateProjectCommand> validator)
{
    public async Task<UpdateProjectResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        UpdateProjectCommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);

        var actor = await projectRepository.GetActorByIdAsync(actorUserId, cancellationToken);
        actor = EnsureActorCanUpdateProject(actor);

        var project = await projectRepository.GetProjectByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            throw new ConflictAppException("Project was not found.", "PROJECT_NOT_FOUND");
        }

        EnsureCompanyScope(actor, project.CompanyId);

        var countryId = command.CountryId ?? project.CountryId;
        var departmentId = command.DepartmentId ?? project.DepartmentId;
        var municipalityId = command.MunicipalityId ?? project.MunicipalityId;
        var baseCurrencyId = command.BaseCurrencyId ?? project.BaseCurrencyId;

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

        try
        {
            project.Code = Project.NormalizeCode(command.Code);
            project.Name = Project.NormalizeName(command.Name);
            project.Description = Project.NormalizeDescription(command.Description);
            project.Objective = Project.NormalizeObjective(command.Objective);
            project.CountryId = countryId;
            project.DepartmentId = departmentId;
            project.MunicipalityId = municipalityId;
            project.BaseCurrencyId = baseCurrencyId;
            project.AiuAdminPercent = command.AiuAdminPercent.HasValue
                ? Project.NormalizeAiuPercent(command.AiuAdminPercent, "aiu_admin_percent")
                : project.AiuAdminPercent;
            project.AiuUnforeseenPercent = command.AiuUnforeseenPercent.HasValue
                ? Project.NormalizeAiuPercent(command.AiuUnforeseenPercent, "aiu_unforeseen_percent")
                : project.AiuUnforeseenPercent;
            project.AiuProfitPercent = command.AiuProfitPercent.HasValue
                ? Project.NormalizeAiuPercent(command.AiuProfitPercent, "aiu_profit_percent")
                : project.AiuProfitPercent;

            if (!string.IsNullOrWhiteSpace(command.SigningMode))
            {
                project.SigningMode = Project.NormalizeSigningModeOrDefault(command.SigningMode);
            }
        }
        catch (InvalidOperationException)
        {
            throw new ValidationAppException("Project payload contains invalid values.", "VALIDATION_ERROR");
        }

        project.SignerUserId = command.SignerUserId;
        project.LastModifiedByUserId = actor.Id;
        project.LastModifiedAtUtc = DateTime.UtcNow;
        project.UpdatedAtUtc = DateTime.UtcNow;

        await projectRepository.SaveChangesAsync(cancellationToken);

        await securityAuditService.WriteAsync(
            new SecurityAuditWriteRequest(
                actor.Id,
                project.CompanyId,
                SecurityAuditEventType.ProjectAdministration,
                SecurityAuditResult.Success,
                PermissionKey: "PROJECTS_UPDATE",
                TargetType: nameof(Project),
                TargetId: project.Id,
                ReasonCode: "PROJECT_UPDATED"),
            cancellationToken);

        return new UpdateProjectResponse
        {
            ProjectId = project.Id,
            UpdatedAtUtc = project.UpdatedAtUtc
        };
    }

    private static UserAccount EnsureActorCanUpdateProject(UserAccount? actor)
    {
        var resolvedActor = actor ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!resolvedActor.AccountEnabled || resolvedActor.AccountLocked || !resolvedActor.Role.IsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        if (resolvedActor.Role.Code is not ProjectStatusTransitionPolicy.AdministratorRoleCode and not ProjectStatusTransitionPolicy.ProjectManagerRoleCode and not ProjectStatusTransitionPolicy.EstimatorRoleCode)
        {
            throw new ForbiddenAppException("Actor cannot update projects.", "PROJECT_UPDATE_FORBIDDEN");
        }

        return resolvedActor;
    }

    private static void EnsureCompanyScope(UserAccount actor, Guid projectCompanyId)
    {
        if (actor.Role.Code == ProjectStatusTransitionPolicy.AdministratorRoleCode)
        {
            return;
        }

        if (!actor.CompanyId.HasValue || actor.CompanyId.Value != projectCompanyId)
        {
            throw new ForbiddenAppException("Actor cannot access project from another company.", "PROJECT_SCOPE_FORBIDDEN");
        }
    }

    private async Task ValidateAsync(UpdateProjectCommand command, CancellationToken cancellationToken)
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
