using BudgetCivil.Application.Users.Bootstrap.Dtos;
using BudgetCivil.Application.Users.Bootstrap.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/admin/bootstrap")]
public sealed class AdminBootstrapController(BootstrapFirstAdministratorUseCase bootstrapFirstAdministratorUseCase) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("first-administrator")]
    [ProducesResponseType(typeof(BootstrapResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<BootstrapResponse>> CreateFirstAdministrator(
        [FromBody] BootstrapRequest request,
        CancellationToken cancellationToken)
    {
        var response = await bootstrapFirstAdministratorUseCase.ExecuteAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
