using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Application.Apus.Commands.WbsItemApu;
using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Wbs.Commands.CreateWbsChapter;
using BudgetCivil.Application.Wbs.Commands.DeleteWbsChapter;
using BudgetCivil.Application.Wbs.Commands.DeleteWbsItem;
using BudgetCivil.Application.Wbs.Commands.MoveWbsNode;
using BudgetCivil.Application.Wbs.Commands.ReorderWbsNodes;
using BudgetCivil.Application.Wbs.Commands.UpdateWbsChapter;
using BudgetCivil.Application.Wbs.Dtos;
using BudgetCivil.Application.Wbs.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

/// <summary>
/// Superficie v1 del EDT (`RF-04`, `D-55`, `D-102`). Doce operaciones project-scoped con `WBS_READ` y
/// `WBS_WRITE`; el alcance por compañía y proyecto se valida en los casos de uso.
/// </summary>
[ApiController]
[Route("api/v1/projects/{projectId:guid}/wbs")]
public sealed class WbsController(
    GetWbsTreeQueryHandler getWbsTreeQueryHandler,
    SearchWbsQueryHandler searchWbsQueryHandler,
    GetWbsActivityQueryHandler getWbsActivityQueryHandler,
    ValidateWbsCodeQueryHandler validateWbsCodeQueryHandler,
    CreateWbsChapterCommandHandler createWbsChapterCommandHandler,
    UpdateWbsChapterCommandHandler updateWbsChapterCommandHandler,
    DeleteWbsChapterCommandHandler deleteWbsChapterCommandHandler,
    CreateWbsItemCommandHandler createWbsItemCommandHandler,
    UpdateWbsItemCommandHandler updateWbsItemCommandHandler,
    DeleteWbsItemCommandHandler deleteWbsItemCommandHandler,
    ReorderWbsNodesCommandHandler reorderWbsNodesCommandHandler,
    MoveWbsNodeCommandHandler moveWbsNodeCommandHandler) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.WbsRead)]
    [ProducesResponseType(typeof(GetWbsTreeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetWbsTreeResponse>> GetTree(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var response = await getWbsTreeQueryHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("search")]
    [Authorize(Policy = AuthorizationPolicies.WbsRead)]
    [ProducesResponseType(typeof(SearchWbsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchWbsResponse>> Search(
        [FromRoute] Guid projectId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await searchWbsQueryHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            new SearchWbsQuery
            {
                Search = search,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("activity")]
    [Authorize(Policy = AuthorizationPolicies.WbsRead)]
    [ProducesResponseType(typeof(GetWbsActivityResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetWbsActivityResponse>> GetActivity(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var response = await getWbsActivityQueryHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("codes/validate")]
    [Authorize(Policy = AuthorizationPolicies.WbsWrite)]
    [ProducesResponseType(typeof(ValidateWbsCodeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ValidateWbsCodeResponse>> ValidateCode(
        [FromRoute] Guid projectId,
        [FromQuery] string? code,
        [FromQuery] Guid? excludeNodeId,
        CancellationToken cancellationToken)
    {
        var response = await validateWbsCodeQueryHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            new ValidateWbsCodeQuery
            {
                Code = code,
                ExcludeNodeId = excludeNodeId
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("chapters")]
    [Authorize(Policy = AuthorizationPolicies.WbsWrite)]
    [ProducesResponseType(typeof(WbsNodeDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<WbsNodeDto>> CreateChapter(
        [FromRoute] Guid projectId,
        [FromBody] CreateWbsChapterCommand command,
        CancellationToken cancellationToken)
    {
        var response = await createWbsChapterCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            command,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("chapters/{chapterId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.WbsWrite)]
    [ProducesResponseType(typeof(WbsNodeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WbsNodeDto>> UpdateChapter(
        [FromRoute] Guid projectId,
        [FromRoute] Guid chapterId,
        [FromBody] UpdateWbsChapterCommand command,
        CancellationToken cancellationToken)
    {
        var response = await updateWbsChapterCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            chapterId,
            command,
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("chapters/{chapterId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.WbsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteChapter(
        [FromRoute] Guid projectId,
        [FromRoute] Guid chapterId,
        [FromQuery] string? rowVersion,
        CancellationToken cancellationToken)
    {
        await deleteWbsChapterCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            chapterId,
            new DeleteWbsChapterCommand
            {
                RowVersion = rowVersion
            },
            cancellationToken);

        return NoContent();
    }

    [HttpPost("items")]
    [Authorize(Policy = AuthorizationPolicies.WbsWrite)]
    [Authorize(Policy = AuthorizationPolicies.ApusWrite)]
    [ProducesResponseType(typeof(WbsItemApuResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<WbsItemApuResponse>> CreateItem(
        [FromRoute] Guid projectId,
        [FromBody] CreateWbsItemCommand command,
        CancellationToken cancellationToken)
    {
        var response = await createWbsItemCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            command,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("items/{itemId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.WbsWrite)]
    [Authorize(Policy = AuthorizationPolicies.ApusWrite)]
    [ProducesResponseType(typeof(WbsItemApuResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<WbsItemApuResponse>> UpdateItem(
        [FromRoute] Guid projectId,
        [FromRoute] Guid itemId,
        [FromBody] UpdateWbsItemCommand command,
        CancellationToken cancellationToken)
    {
        var response = await updateWbsItemCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            itemId,
            command,
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("items/{itemId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.WbsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteItem(
        [FromRoute] Guid projectId,
        [FromRoute] Guid itemId,
        [FromQuery] string? rowVersion,
        CancellationToken cancellationToken)
    {
        await deleteWbsItemCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            itemId,
            new DeleteWbsItemCommand { RowVersion = rowVersion },
            cancellationToken);

        return NoContent();
    }


    [HttpPost("reorder")]
    [Authorize(Policy = AuthorizationPolicies.WbsWrite)]
    [ProducesResponseType(typeof(ReorderWbsNodesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReorderWbsNodesResponse>> Reorder(
        [FromRoute] Guid projectId,
        [FromBody] ReorderWbsNodesCommand command,
        CancellationToken cancellationToken)
    {
        var response = await reorderWbsNodesCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            command,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("move")]
    [Authorize(Policy = AuthorizationPolicies.WbsWrite)]
    [ProducesResponseType(typeof(WbsNodeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WbsNodeDto>> Move(
        [FromRoute] Guid projectId,
        [FromBody] MoveWbsNodeCommand command,
        CancellationToken cancellationToken)
    {
        var response = await moveWbsNodeCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            command,
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
