using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BudgetCivil.Backend.BlackBoxTests.Infrastructure;

public sealed class AuthenticatedClientBuilder(TestWebApplicationFactory factory)
{
    private Guid _userId = Guid.NewGuid();
    private string _roleCode = "ADMINISTRADOR";
    private Guid? _companyId;
    private List<string> _permissions = [];

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

    public HttpClient Build()
    {
        var client = factory.CreateClient();
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
