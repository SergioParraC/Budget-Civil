using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/resource-types")]
public sealed class ResourceTypesController(GetResourceTypesQueryHandler getResourceTypesQueryHandler) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ResourcesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceTypeOptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResourceTypeOptionDto>>> GetResourceTypes(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await getResourceTypesQueryHandler.ExecuteAsync(
            actorUserId,
            projectId,
            cancellationToken);

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