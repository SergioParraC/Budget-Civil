using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Profile.Dtos;
using BudgetCivil.Application.Profile.UseCases;
using BudgetCivil.Application.Users.Administration.Dtos;
using BudgetCivil.Application.Users.Administration.Guards;
using BudgetCivil.Application.Users.Administration.UseCases;
using BudgetCivil.Application.Users.Search;
using BudgetCivil.Application.Users.Search.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(
    CreateUserUseCase createUserUseCase,
    UpdateUserUseCase updateUserUseCase,
    DeactivateUserUseCase deactivateUserUseCase,
    ChangeUserRoleUseCase changeUserRoleUseCase,
    ChangeUserCompanyUseCase changeUserCompanyUseCase,
    EnableUserPasswordRecoveryUseCase enableUserPasswordRecoveryUseCase,
    SearchUsersUseCase searchUsersUseCase,
    RejectProjectAssignmentGuard rejectProjectAssignmentGuard) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.UsersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<UserSearchResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSearchResult>>> SearchUsers(
        [FromQuery(Name = "email")] string? email,
        [FromQuery(Name = "username")] string? username,
        [FromQuery(Name = "nombre_apellidos")] string? fullName,
        [FromQuery(Name = "numero_documento")] string? documentNumber,
        [FromQuery(Name = "estado_cuenta")] bool? accountEnabled,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();

        var query = new UserSearchQuery
        {
            Email = email,
            Username = username,
            FullName = fullName,
            DocumentNumber = documentNumber,
            AccountEnabled = accountEnabled
        };

        var response = await searchUsersUseCase.ExecuteAsync(actorUserId, query, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.UsersCreate)]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateUserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        rejectProjectAssignmentGuard.EnsureNoProjectAssignment(request.ExtensionData);

        var actorUserId = GetActorUserId();
        var response = await createUserUseCase.ExecuteAsync(actorUserId, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{userId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.UsersUpdate)]
    [ProducesResponseType(typeof(UpdateUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateUserResponse>> UpdateUser(
        [FromRoute] Guid userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        rejectProjectAssignmentGuard.EnsureNoProjectAssignment(request.ExtensionData);

        var actorUserId = GetActorUserId();
        var response = await updateUserUseCase.ExecuteAsync(actorUserId, userId, request, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{userId:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.UsersDeactivate)]
    [ProducesResponseType(typeof(DeactivateUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeactivateUserResponse>> DeactivateUser(
        [FromRoute] Guid userId,
        [FromBody] DeactivateUserRequest request,
        CancellationToken cancellationToken)
    {
        rejectProjectAssignmentGuard.EnsureNoProjectAssignment(request.ExtensionData);

        var actorUserId = GetActorUserId();
        var response = await deactivateUserUseCase.ExecuteAsync(actorUserId, userId, request, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{userId:guid}/role")]
    [Authorize(Policy = AuthorizationPolicies.UsersRoleChange)]
    [ProducesResponseType(typeof(ChangeUserRoleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChangeUserRoleResponse>> ChangeUserRole(
        [FromRoute] Guid userId,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        rejectProjectAssignmentGuard.EnsureNoProjectAssignment(request.ExtensionData);

        var actorUserId = GetActorUserId();
        var response = await changeUserRoleUseCase.ExecuteAsync(actorUserId, userId, request, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{userId:guid}/company")]
    [Authorize(Policy = AuthorizationPolicies.UsersCompanyChange)]
    [ProducesResponseType(typeof(ChangeUserCompanyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChangeUserCompanyResponse>> ChangeUserCompany(
        [FromRoute] Guid userId,
        [FromBody] ChangeUserCompanyRequest request,
        CancellationToken cancellationToken)
    {
        rejectProjectAssignmentGuard.EnsureNoProjectAssignment(request.ExtensionData);

        var actorUserId = GetActorUserId();
        var response = await changeUserCompanyUseCase.ExecuteAsync(actorUserId, userId, request, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{userId:guid}/password-recovery/enable")]
    [Authorize(Policy = AuthorizationPolicies.UsersPasswordRecoveryEnable)]
    [ProducesResponseType(typeof(EnableUserPasswordRecoveryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EnableUserPasswordRecoveryResponse>> EnablePasswordRecovery(
        [FromRoute] Guid userId,
        [FromBody] EnableUserPasswordRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await enableUserPasswordRecoveryUseCase.ExecuteAsync(actorUserId, userId, request, cancellationToken);

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
