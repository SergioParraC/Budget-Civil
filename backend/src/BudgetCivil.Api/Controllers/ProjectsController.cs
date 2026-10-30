using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Memberships.Projects;
using BudgetCivil.Application.Projects.Commands.ChangeProjectStatus;
using BudgetCivil.Application.Projects.Commands.ChangeProjectVisibility;
using BudgetCivil.Application.Projects.Commands.CreateProject;
using BudgetCivil.Application.Projects.Commands.UpdateProject;
using BudgetCivil.Application.Projects.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/projects")]
public sealed class ProjectsController(
    CreateProjectCommandHandler createProjectCommandHandler,
    UpdateProjectCommandHandler updateProjectCommandHandler,
    ChangeProjectStatusCommandHandler changeProjectStatusCommandHandler,
    ChangeProjectVisibilityCommandHandler changeProjectVisibilityCommandHandler,
    GetScopedProjectsQueryHandler getScopedProjectsQueryHandler,
    GetProjectByIdQueryHandler getProjectByIdQueryHandler,
    GetProjectConfigurationQueryHandler getProjectConfigurationQueryHandler,
    AssignProjectMemberCommandHandler assignProjectMemberCommandHandler,
    DeactivateProjectMemberCommandHandler deactivateProjectMemberCommandHandler) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ProjectsCreate)]
    [ProducesResponseType(typeof(CreateProjectResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateProjectResponse>> CreateProject(
        [FromBody] CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await createProjectCommandHandler.ExecuteAsync(actorUserId, command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{projectId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsUpdate)]
    [ProducesResponseType(typeof(UpdateProjectResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateProjectResponse>> UpdateProject(
        [FromRoute] Guid projectId,
        [FromBody] UpdateProjectCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await updateProjectCommandHandler.ExecuteAsync(actorUserId, projectId, command, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{projectId:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsStatusChange)]
    [ProducesResponseType(typeof(ChangeProjectStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChangeProjectStatusResponse>> ChangeStatus(
        [FromRoute] Guid projectId,
        [FromBody] ChangeProjectStatusCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await changeProjectStatusCommandHandler.ExecuteAsync(actorUserId, projectId, command, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{projectId:guid}/status/reactivate")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsStatusChange)]
    [ProducesResponseType(typeof(ChangeProjectStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChangeProjectStatusResponse>> ReactivateStatus(
        [FromRoute] Guid projectId,
        [FromBody] ChangeProjectStatusCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await changeProjectStatusCommandHandler.ReactivateAsync(actorUserId, projectId, command, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{projectId:guid}/visibility")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsVisibilityChange)]
    [ProducesResponseType(typeof(ChangeProjectVisibilityResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChangeProjectVisibilityResponse>> ChangeVisibility(
        [FromRoute] Guid projectId,
        [FromBody] ChangeProjectVisibilityCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await changeProjectVisibilityCommandHandler.ExecuteAsync(actorUserId, projectId, command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{projectId:guid}/memberships")]
    [Authorize(Policy = AuthorizationPolicies.ProjectMembersAssign)]
    [ProducesResponseType(typeof(AssignProjectMemberResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssignProjectMemberResponse>> AssignProjectMember(
        [FromRoute] Guid projectId,
        [FromBody] AssignProjectMemberCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await assignProjectMemberCommandHandler.ExecuteAsync(actorUserId, projectId, command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("{projectId:guid}/memberships/{membershipId:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.ProjectMembersDeactivate)]
    [ProducesResponseType(typeof(DeactivateProjectMemberResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeactivateProjectMemberResponse>> DeactivateProjectMember(
        [FromRoute] Guid projectId,
        [FromRoute] Guid membershipId,
        [FromBody] DeactivateProjectMemberCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await deactivateProjectMemberCommandHandler.ExecuteAsync(actorUserId, projectId, membershipId, command, cancellationToken);

        return Ok(response);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ProjectsRead)]
    [ProducesResponseType(typeof(GetScopedProjectsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetScopedProjectsResponse>> GetProjects(
        [FromQuery] Guid? companyId,
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = GetActorUserId();
        var response = await getScopedProjectsQueryHandler.ExecuteAsync(
            actorUserId,
            new GetScopedProjectsQuery
            {
                CompanyId = companyId,
                Status = status,
                Search = search,
                Page = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{projectId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsRead)]
    [ProducesResponseType(typeof(GetProjectByIdResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetProjectByIdResponse>> GetProjectById(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await getProjectByIdQueryHandler.ExecuteAsync(actorUserId, projectId, cancellationToken);

        return Ok(response);
    }

    [HttpGet("{projectId:guid}/configuration")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsRead)]
    [ProducesResponseType(typeof(GetProjectConfigurationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetProjectConfigurationResponse>> GetProjectConfiguration(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await getProjectConfigurationQueryHandler.ExecuteAsync(actorUserId, projectId, cancellationToken);

        return Ok(response);
    }

    private Guid GetActorUserId()
    {
        var actorUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(actorUserIdClaim, out var actorUserId))
        {
            throw new ForbiddenAppException("Authenticated user context is invalid.", "INVALID_AUTH_SUBJECT");
        }

        return actorUserId;
    }
}
