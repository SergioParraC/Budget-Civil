using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BudgetCivil.Backend.BlackBoxTests.Infrastructure.Authentication;

public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestAuth";
    public const string UserIdHeaderName = "X-Test-User-Id";
    public const string RoleCodeHeaderName = "X-Test-Role-Code";
    public const string CompanyIdHeaderName = "X-Test-Company-Id";
    public const string PermissionKeysHeaderName = "X-Test-Permissions";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var hasRoleHeader = Request.Headers.TryGetValue(RoleCodeHeaderName, out var roleCodeValues);
        var hasUserHeader = Request.Headers.TryGetValue(UserIdHeaderName, out var userIdValues);
        var roleCode = roleCodeValues.ToString();
        var userIdHeader = userIdValues.ToString();

        if (!hasRoleHeader && !hasUserHeader)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!hasRoleHeader || string.IsNullOrWhiteSpace(roleCode))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing required header {RoleCodeHeaderName}."));
        }

        if (!hasUserHeader || !Guid.TryParse(userIdHeader, out var userId))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing or invalid header {UserIdHeaderName}."));
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("role_code", roleCode.Trim())
        };

        if (Request.Headers.TryGetValue(CompanyIdHeaderName, out var companyIdValues) &&
            Guid.TryParse(companyIdValues.ToString(), out var companyId))
        {
            claims.Add(new Claim("company_id", companyId.ToString()));
        }

        if (Request.Headers.TryGetValue(PermissionKeysHeaderName, out var permissionValues))
        {
            var permissions = permissionValues
                .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.Ordinal);

            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission_key", permission));
            }
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
