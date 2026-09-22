using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BudgetCivil.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions) : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public AccessTokenResult GenerateToken(UserAccount userAccount, IEnumerable<string> permissionKeys)
    {
        var issuedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(_jwtOptions.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userAccount.Id.ToString()),
            new(ClaimTypes.NameIdentifier, userAccount.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, userAccount.Email),
            new(JwtRegisteredClaimNames.UniqueName, userAccount.Username),
            new("role_code", userAccount.Role.Code)
        };

        if (userAccount.CompanyId.HasValue)
        {
            claims.Add(new Claim("company_id", userAccount.CompanyId.Value.ToString()));
        }

        claims.AddRange(permissionKeys.Select(permissionKey => new Claim("permission_key", permissionKey)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenResult(tokenValue, expiresAtUtc);
    }
}
