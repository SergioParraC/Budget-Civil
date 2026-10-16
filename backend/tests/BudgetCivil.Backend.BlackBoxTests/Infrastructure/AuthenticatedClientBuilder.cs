using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using BudgetCivil.Backend.BlackBoxTests.Infrastructure.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace BudgetCivil.Backend.BlackBoxTests.Infrastructure;

public sealed class AuthenticatedClientBuilder(TestWebApplicationFactory factory)
{
    private Guid _userId = Guid.NewGuid();
    private string _roleCode = "ADMINISTRADOR";
    private Guid? _companyId;
    private List<string> _permissions = [];
    private bool _useTestAuthHeaders;

    public AuthenticatedClientBuilder WithUser(Guid userId, string roleCode, Guid? companyId = null)
    {
        _userId = userId;
        _roleCode = roleCode;
        _companyId = companyId;
        return this;
    }

    public AuthenticatedClientBuilder WithPermissions(IEnumerable<string> permissionKeys)
    {
        _permissions = permissionKeys.Distinct(StringComparer.Ordinal).ToList();
        return this;
    }

    public AuthenticatedClientBuilder UseTestAuthHeaders(bool enabled = true)
    {
        _useTestAuthHeaders = enabled;
        return this;
    }

    public HttpClient Build()
    {
        var client = factory.CreateClient();

        if (_useTestAuthHeaders)
        {
            client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeaderName);
            client.DefaultRequestHeaders.Remove(TestAuthHandler.RoleCodeHeaderName);
            client.DefaultRequestHeaders.Remove(TestAuthHandler.CompanyIdHeaderName);
            client.DefaultRequestHeaders.Remove(TestAuthHandler.PermissionKeysHeaderName);

            client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, _userId.ToString());
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleCodeHeaderName, _roleCode);

            if (_companyId.HasValue)
            {
                client.DefaultRequestHeaders.Add(TestAuthHandler.CompanyIdHeaderName, _companyId.Value.ToString());
            }

            if (_permissions.Count > 0)
            {
                client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionKeysHeaderName, string.Join(',', _permissions));
            }

            return client;
        }

        var token = BuildJwtToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private string BuildJwtToken()
    {
        const string issuer = "BudgetCivil.Api";
        const string audience = "BudgetCivil.Client";
        const string signingKey = "CHANGE_ME_WITH_A_MINIMUM_32_CHAR_SECRET";

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, _userId.ToString()),
            new(ClaimTypes.NameIdentifier, _userId.ToString()),
            new("role_code", _roleCode)
        };

        if (_companyId.HasValue)
        {
            claims.Add(new Claim("company_id", _companyId.Value.ToString()));
        }

        claims.AddRange(_permissions.Select(permission => new Claim("permission_key", permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            now,
            now.AddMinutes(30),
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwtToken);
    }
}
