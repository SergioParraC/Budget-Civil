using BudgetCivil.Api.Auth;
using BudgetCivil.Application.Users.Auth.Dtos;
using BudgetCivil.Application.Users.Auth.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    RegisterUserUseCase registerUserUseCase,
    LoginUseCase loginUseCase,
    ConfirmEmailVerificationUseCase confirmEmailVerificationUseCase,
    RequestPasswordRecoveryUseCase requestPasswordRecoveryUseCase,
    ConfirmPasswordRecoveryUseCase confirmPasswordRecoveryUseCase) : ControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Registration)]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<RegisterUserResponse>> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var response = await registerUserUseCase.ExecuteAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>REQ-IDENTIDAD-07: consume el token enviado por correo y habilita la cuenta.</summary>
    [AllowAnonymous]
    [HttpPost("email-verification/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConfirmEmailVerification(
        [FromBody] EmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        await confirmEmailVerificationUseCase.ExecuteAsync(request.Token, cancellationToken);
        return NoContent();
    }

    /// <summary>REQ-IDENTIDAD-25: solicitud anonima; la respuesta no revela si el correo existe.</summary>
    [AllowAnonymous]
    [HttpPost("password-recovery")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RequestPasswordRecovery(
        [FromBody] RequestPasswordRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        await requestPasswordRecoveryUseCase.ExecuteAsync(request.Email, cancellationToken);
        return Accepted();
    }

    /// <summary>REQ-IDENTIDAD-25: consume el token una sola vez y aplica la contrasena nueva.</summary>
    [AllowAnonymous]
    [HttpPost("password-recovery/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConfirmPasswordRecovery(
        [FromBody] ConfirmPasswordRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        await confirmPasswordRecoveryUseCase.ExecuteAsync(request.Token, request.NewPassword, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("login")]    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await loginUseCase.ExecuteAsync(request, cancellationToken);
        return Ok(response);
    }
}
