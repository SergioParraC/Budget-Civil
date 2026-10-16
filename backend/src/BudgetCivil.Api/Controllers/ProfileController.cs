using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Profile.Dtos;
using BudgetCivil.Application.Profile.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/profile/me")]
public sealed class ProfileController(
    GetMyProfileUseCase getMyProfileUseCase,
    UpdateMyProfileUseCase updateMyProfileUseCase,
    ChangeMyPasswordUseCase changeMyPasswordUseCase) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ProfileReadSelf)]
    [ProducesResponseType(typeof(GetMyProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetMyProfileResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await getMyProfileUseCase.ExecuteAsync(actorUserId, cancellationToken);

        return Ok(response);
    }

    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.ProfileUpdateSelf)]
    [ProducesResponseType(typeof(UpdateMyProfileResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateMyProfileResponse>> UpdateMyProfile(
        [FromBody] UpdateMyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await updateMyProfileUseCase.ExecuteAsync(actorUserId, request, cancellationToken);

        return Ok(response);
    }

    [HttpPost("change-password")]
    [Authorize(Policy = AuthorizationPolicies.ProfileChangePasswordSelf)]
    [ProducesResponseType(typeof(ChangeMyPasswordResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChangeMyPasswordResponse>> ChangeMyPassword(
        [FromBody] ChangeMyPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await changeMyPasswordUseCase.ExecuteAsync(actorUserId, request, cancellationToken);

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
