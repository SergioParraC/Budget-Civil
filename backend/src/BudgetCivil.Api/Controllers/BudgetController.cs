using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Application.Calculation.Dtos;
using BudgetCivil.Application.Calculation.Queries;
using BudgetCivil.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

/// <summary>
/// Superficie v1 del presupuesto (`RF-10`, `RF-17`, `D4`). Es una consulta en solo lectura con el mismo
/// permiso y alcance que el árbol del EDT.
/// </summary>
[ApiController]
[Route("api/v1/projects/{projectId:guid}/budget")]
public sealed class BudgetController(GetProjectBudgetQueryHandler getProjectBudgetQueryHandler) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.WbsRead)]
    [ProducesResponseType(typeof(ProjectBudgetResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectBudgetResponse>> GetBudget(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var response = await getProjectBudgetQueryHandler.ExecuteAsync(
            GetActorUserId(),
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
