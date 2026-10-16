using BudgetCivil.Api.Auth;
using BudgetCivil.Api.Middleware;
using BudgetCivil.Application.Users.Bootstrap.Dtos;
using BudgetCivil.Application.Users.Bootstrap.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/audit")]
public sealed class AuditController(GetBootstrapAuditUseCase getBootstrapAuditUseCase) : ControllerBase
{
    [HttpGet("bootstrap")]
    [Authorize(Policy = AuthorizationPolicies.BootstrapAuditRead)]
    [AllowWithoutCompanyContext]
    [ProducesResponseType(typeof(IReadOnlyList<BootstrapAuditRecordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BootstrapAuditRecordResponse>>> GetBootstrapAudit(
        CancellationToken cancellationToken)
    {
        var response = await getBootstrapAuditUseCase.ExecuteAsync(cancellationToken);
        return Ok(response);
    }
}
