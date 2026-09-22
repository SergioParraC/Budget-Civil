using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Api.Middleware;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Companies.Dtos;
using BudgetCivil.Application.Companies.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/companies")]
public sealed class CompaniesController(
    CreateCompanyUseCase createCompanyUseCase,
    UpdateCompanyUseCase updateCompanyUseCase,
    DeactivateCompanyUseCase deactivateCompanyUseCase) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CompaniesCreate)]
    [AllowWithoutCompanyContext]
    [ProducesResponseType(typeof(CreateCompanyResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateCompanyResponse>> CreateCompany(
        [FromBody] CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await createCompanyUseCase.ExecuteAsync(actorUserId, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{companyId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CompaniesUpdate)]
    [ProducesResponseType(typeof(UpdateCompanyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateCompanyResponse>> UpdateCompany(
        [FromRoute] Guid companyId,
        [FromBody] UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await updateCompanyUseCase.ExecuteAsync(actorUserId, companyId, request, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{companyId:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.CompaniesDeactivate)]
    [ProducesResponseType(typeof(DeactivateCompanyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeactivateCompanyResponse>> DeactivateCompany(
        [FromRoute] Guid companyId,
        [FromBody] DeactivateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await deactivateCompanyUseCase.ExecuteAsync(actorUserId, companyId, request, cancellationToken);

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
