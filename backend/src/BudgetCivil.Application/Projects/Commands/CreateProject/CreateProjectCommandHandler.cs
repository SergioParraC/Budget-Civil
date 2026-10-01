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

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("objective")]
    public string? Objective { get; init; }

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

        RuleFor(command => command.Objective)
            .MaximumLength(Project.ObjectiveMaxLength)
            .When(command => !string.IsNullOrWhiteSpace(command.Objective));

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
                Name = Project.NormalizeName(command.Name),
                Description = Project.NormalizeDescription(command.Description),
                Objective = Project.NormalizeObjective(command.Objective),
                Status = ProjectStatus.PorCotizar,
                IsPublic = false,
                SigningMode = Project.NormalizeSigningModeOrDefault(command.SigningMode),
                SignerUserId = command.SignerUserId,
                DirectorOwnerUserId = directorOwnerUserId,
                CreatedByUserId = actor.Id,
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
