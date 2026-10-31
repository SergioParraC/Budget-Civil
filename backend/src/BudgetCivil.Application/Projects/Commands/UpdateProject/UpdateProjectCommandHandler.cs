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

public sealed class UpdateProjectResponse
{
    public Guid ProjectId { get; init; }

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
                Project.IsValidSignatureMode(signingMode))
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

        var configuration = project.Configuration
            ?? throw new ConflictAppException("Project configuration was not found.", "PROJECT_CONFIGURATION_NOT_FOUND");

        // `D-112`: el Director es el único que configura los parámetros del proyecto. El Presupuestador
        // conserva la edición de datos generales (nombre, código, descripción y objetivo).
        if (actor.Role.Code == ProjectStatusTransitionPolicy.EstimatorRoleCode)
        {
            var requestedSignatureMode = string.IsNullOrWhiteSpace(command.SigningMode)
                ? null
                : Project.NormalizeSignatureModeOrDefault(command.SigningMode);

            var changesSignatureMode = requestedSignatureMode is not null &&
                !string.Equals(requestedSignatureMode, configuration.SignatureMode, StringComparison.Ordinal);

            var changesSigner = command.SignerUserId != configuration.SignerUserId;

            var changesAuditorRequirement = command.RequiresAuditor.HasValue &&
                command.RequiresAuditor.Value != configuration.RequiresAuditor;

            if (changesSignatureMode || changesSigner || changesAuditorRequirement)
            {
                throw new ForbiddenAppException(
                    "Only Director de proyectos can change the signature mode, the signer or the auditor requirement.",
                    "PROJECT_SIGNATURE_CONFIGURATION_FORBIDDEN");
            }

            var changesLocation =
                (command.CountryId.HasValue && command.CountryId != project.CountryId) ||
                (command.DepartmentId.HasValue && command.DepartmentId != project.DepartmentId) ||
                (command.MunicipalityId.HasValue && command.MunicipalityId != project.MunicipalityId);

            var changesFinancialConfiguration =
                (command.BaseCurrencyId.HasValue && command.BaseCurrencyId != configuration.BaseCurrencyId) ||
                (command.BudgetLimit.HasValue && command.BudgetLimit != configuration.BudgetLimit) ||
                (command.AiuAdminPercent.HasValue && command.AiuAdminPercent != configuration.AiuAdminPercent) ||
                (command.AiuUnforeseenPercent.HasValue && command.AiuUnforeseenPercent != configuration.AiuUnforeseenPercent) ||
                (command.AiuProfitPercent.HasValue && command.AiuProfitPercent != configuration.AiuProfitPercent);

            if (changesLocation || changesFinancialConfiguration)
            {
                throw new ForbiddenAppException(
                    "Only Director de proyectos can change the project configuration.",
                    "PROJECT_CONFIGURATION_FORBIDDEN");
            }
        }

        var countryId = command.CountryId ?? project.CountryId;
        var departmentId = command.DepartmentId ?? project.DepartmentId;
        var municipalityId = command.MunicipalityId ?? project.MunicipalityId;
        var baseCurrencyId = command.BaseCurrencyId ?? configuration.BaseCurrencyId;

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
            configuration.BaseCurrencyId = baseCurrencyId;
            configuration.BudgetLimit = command.BudgetLimit.HasValue
                ? ProjectConfiguration.NormalizeBudgetLimit(command.BudgetLimit)
                : configuration.BudgetLimit;
            configuration.AiuAdminPercent = command.AiuAdminPercent.HasValue
                ? ProjectConfiguration.NormalizeAiuPercent(command.AiuAdminPercent, "aiu_admin_percent")
                : configuration.AiuAdminPercent;
            configuration.AiuUnforeseenPercent = command.AiuUnforeseenPercent.HasValue
                ? ProjectConfiguration.NormalizeAiuPercent(command.AiuUnforeseenPercent, "aiu_unforeseen_percent")
                : configuration.AiuUnforeseenPercent;
            configuration.AiuProfitPercent = command.AiuProfitPercent.HasValue
                ? ProjectConfiguration.NormalizeAiuPercent(command.AiuProfitPercent, "aiu_profit_percent")
                : configuration.AiuProfitPercent;

            if (!string.IsNullOrWhiteSpace(command.SigningMode))
            {
                configuration.SignatureMode = Project.NormalizeSignatureModeOrDefault(command.SigningMode);
            }

            if (command.RequiresAuditor.HasValue)
            {
                configuration.RequiresAuditor = command.RequiresAuditor.Value;
            }
        }
        catch (InvalidOperationException)
        {
            throw new ValidationAppException("Project payload contains invalid values.", "VALIDATION_ERROR");
        }

        configuration.SignerUserId = command.SignerUserId;
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

        if (resolvedActor.Role.Code is not ProjectStatusTransitionPolicy.AdministratorRoleCode and not ProjectStatusTransitionPolicy.DirectorRoleCode and not ProjectStatusTransitionPolicy.EstimatorRoleCode)
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
