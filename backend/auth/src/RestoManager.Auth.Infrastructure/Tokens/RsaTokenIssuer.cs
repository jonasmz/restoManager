using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RestoManager.Auth.Domain.Abstractions;
using RestoManager.Auth.Domain.Tokens;
using RestoManager.Auth.Infrastructure.Setup;

namespace RestoManager.Auth.Infrastructure.Tokens;

public sealed class RsaTokenIssuer(RsaSigningKeyStore keys, AuthOptions options, IClock clock) : ITokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken Issue(AccessTokenClaims claims)
    {
        var now = clock.UtcNow;
        var expires = now.AddMinutes(options.AccessTokenMinutes);

        var identityClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, claims.UserId.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Email, claims.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("employee_id", claims.EmployeeId.ToString(CultureInfo.InvariantCulture)),
        };
        identityClaims.AddRange(claims.Roles.Select(r => new Claim("role", r)));
        identityClaims.AddRange(claims.BranchIds.Select(b => new Claim("branch_id", b.ToString(CultureInfo.InvariantCulture))));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Subject = new ClaimsIdentity(identityClaims),
            SigningCredentials = keys.SigningCredentials,
        };

        return new AccessToken(_handler.CreateToken(descriptor), expires);
    }
}
