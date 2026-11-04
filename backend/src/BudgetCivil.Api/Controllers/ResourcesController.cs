using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Commands.CopyResource;
using BudgetCivil.Application.Resources.Commands.CreateResource;
using BudgetCivil.Application.Resources.Commands.UpdateResource;
using BudgetCivil.Application.Resources.Dtos;
using BudgetCivil.Application.Resources.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/projects/{projectId:guid}/resources")]
public sealed class ResourcesController(
    CreateResourceCommandHandler createResourceCommandHandler,
    UpdateResourceCommandHandler updateResourceCommandHandler,
    CopyResourceCommandHandler copyResourceCommandHandler,
    GetResourcesQueryHandler getResourcesQueryHandler,
    GetResourceByIdQueryHandler getResourceByIdQueryHandler,
    GetResourceSuggestionsQueryHandler getResourceSuggestionsQueryHandler) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ResourcesRead)]
    [ProducesResponseType(typeof(GetResourcesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetResourcesResponse>> GetResources(
        [FromRoute] Guid projectId,
        [FromQuery] string? search,
        [FromQuery] Guid? resourceTypeId,
        [FromQuery] string? quotationStatus,
        [FromQuery] string? brand,
        [FromQuery] Guid? municipalityId,
        [FromQuery] decimal? minUnitPrice,
        [FromQuery] decimal? maxUnitPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = GetActorUserId();
        var response = await getResourcesQueryHandler.ExecuteAsync(
            actorUserId,
            projectId,
            new GetResourcesQuery
            {
                Search = search,
                ResourceTypeId = resourceTypeId,
                QuotationStatus = quotationStatus,
                Brand = brand,
                MunicipalityId = municipalityId,
                MinUnitPrice = minUnitPrice,
                MaxUnitPrice = maxUnitPrice,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDirection = sortDirection
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{resourceId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ResourcesRead)]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResourceDto>> GetResourceById(
        [FromRoute] Guid projectId,
        [FromRoute] Guid resourceId,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await getResourceByIdQueryHandler.ExecuteAsync(
            actorUserId,
            projectId,
            resourceId,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ResourcesWrite)]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ResourceDto>> CreateResource(
        [FromRoute] Guid projectId,
        [FromBody] CreateResourceCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await createResourceCommandHandler.ExecuteAsync(
            actorUserId,
            projectId,
            command,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{resourceId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ResourcesWrite)]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResourceDto>> UpdateResource(
        [FromRoute] Guid projectId,
        [FromRoute] Guid resourceId,
        [FromBody] UpdateResourceCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await updateResourceCommandHandler.ExecuteAsync(
            actorUserId,
            projectId,
            resourceId,
            command,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("copy")]
    [Authorize(Policy = AuthorizationPolicies.ResourcesWrite)]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ResourceDto>> CopyResource(
        [FromRoute] Guid projectId,
        [FromBody] CopyResourceCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await copyResourceCommandHandler.ExecuteAsync(
            actorUserId,
            projectId,
            command,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("suggestions")]
    [Authorize(Policy = AuthorizationPolicies.ResourcesWrite)]
    [ProducesResponseType(typeof(GetResourceSuggestionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetResourceSuggestionsResponse>> GetSuggestions(
        [FromRoute] Guid projectId,
        [FromQuery] string? description,
        [FromQuery] string? userKey,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await getResourceSuggestionsQueryHandler.ExecuteAsync(
            actorUserId,
            projectId,
            new GetResourceSuggestionsQuery
            {
                Description = description,
                UserKey = userKey
            },
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
