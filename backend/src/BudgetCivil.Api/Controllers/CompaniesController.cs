using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Api.Middleware;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Companies.ChangeCompanyStatus;
using BudgetCivil.Application.Companies.CreateCompany;
using BudgetCivil.Application.Companies.Dtos;
using BudgetCivil.Application.Companies.ListCompanies;
using BudgetCivil.Application.Companies.UpdateCompany;
using BudgetCivil.Application.Memberships.Companies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/companies")]
public sealed class CompaniesController(
    CreateCompanyCommandHandler createCompanyCommandHandler,
    UpdateCompanyCommandHandler updateCompanyCommandHandler,
    DisableCompanyCommandHandler disableCompanyCommandHandler,
    EnableCompanyCommandHandler enableCompanyCommandHandler,
    GetCompaniesQueryHandler getCompaniesQueryHandler,
    GetScopedUsersQueryHandler getScopedUsersQueryHandler,
    AssignCompanyMemberCommandHandler assignCompanyMemberCommandHandler,
    DeactivateCompanyMemberCommandHandler deactivateCompanyMemberCommandHandler) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CompaniesCreate)]
    [AllowWithoutCompanyContext]
    [ProducesResponseType(typeof(CreateCompanyResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateCompanyResponse>> CreateCompany(
        [FromBody] CreateCompanyCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await createCompanyCommandHandler.ExecuteAsync(actorUserId, command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{companyId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.CompaniesUpdate)]
    [ProducesResponseType(typeof(UpdateCompanyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateCompanyResponse>> UpdateCompany(
        [FromRoute] Guid companyId,
        [FromBody] UpdateCompanyCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await updateCompanyCommandHandler.ExecuteAsync(actorUserId, companyId, command, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{companyId:guid}/disable")]
    [HttpPatch("{companyId:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.CompaniesDeactivate)]
    [ProducesResponseType(typeof(DeactivateCompanyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeactivateCompanyResponse>> DisableCompany(
        [FromRoute] Guid companyId,
        [FromBody] DisableCompanyCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await disableCompanyCommandHandler.ExecuteAsync(actorUserId, companyId, command, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{companyId:guid}/enable")]
    [Authorize(Policy = AuthorizationPolicies.CompaniesReactivate)]
    [ProducesResponseType(typeof(DeactivateCompanyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeactivateCompanyResponse>> EnableCompany(
        [FromRoute] Guid companyId,
        [FromBody] EnableCompanyCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await enableCompanyCommandHandler.ExecuteAsync(actorUserId, companyId, command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{companyId:guid}/memberships")]
    [Authorize(Policy = AuthorizationPolicies.CompanyMembersAssign)]
    [ProducesResponseType(typeof(AssignCompanyMemberResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AssignCompanyMemberResponse>> AssignCompanyMember(
        [FromRoute] Guid companyId,
        [FromBody] AssignCompanyMemberCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await assignCompanyMemberCommandHandler.ExecuteAsync(actorUserId, companyId, command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("{companyId:guid}/memberships/{membershipId:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.CompanyMembersDeactivate)]
    [ProducesResponseType(typeof(DeactivateCompanyMemberResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeactivateCompanyMemberResponse>> DeactivateCompanyMember(
        [FromRoute] Guid companyId,
        [FromRoute] Guid membershipId,
        [FromBody] DeactivateCompanyMemberCommand command,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await deactivateCompanyMemberCommandHandler.ExecuteAsync(actorUserId, companyId, membershipId, command, cancellationToken);

        return Ok(response);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.CompaniesRead)]
    [ProducesResponseType(typeof(GetCompaniesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetCompaniesResponse>> GetCompanies(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = GetActorUserId();
        var response = await getCompaniesQueryHandler.ExecuteAsync(
            actorUserId,
            new GetCompaniesQuery
            {
                Search = search,
                IsActive = isActive,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{companyId:guid}/users")]
    [Authorize(Policy = AuthorizationPolicies.UsersRead)]
    [ProducesResponseType(typeof(GetScopedUsersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetScopedUsersResponse>> GetCompanyUsers(
        [FromRoute] Guid companyId,
        [FromQuery] string? search,
        [FromQuery] string? roleCode,
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = GetActorUserId();

        var response = await getScopedUsersQueryHandler.ExecuteAsync(
            actorUserId,
            new GetScopedUsersQuery
            {
                Scope = ScopedUsersListingScope.CompanyById,
                CompanyId = companyId,
                Search = search,
                RoleCode = roleCode,
                IsActive = isActive,
                PageNumber = pageNumber,
                PageSize = pageSize
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
