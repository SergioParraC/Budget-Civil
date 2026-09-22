using BudgetCivil.Api.Auth;
using BudgetCivil.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/apu")]
public sealed class PublicApuController : ControllerBase
{
    [HttpGet("public")]
    [Authorize(Policy = AuthorizationPolicies.ApuPublicRead)]
    [AllowWithoutCompanyContext]
    public IActionResult GetPublicApuReadOnlyCatalog()
    {
        var payload = new[]
        {
            new
            {
                apu_id = "APU-DEMO-001",
                nombre = "APU Publico Demo",
                version = "1.0",
                is_public = true
            }
        };

        return Ok(payload);
    }
}
