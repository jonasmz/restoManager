using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using RestoManager.Auth.Domain.Tokens;

namespace RestoManager.Auth.Infrastructure.Tokens;

public sealed class JwksProvider(RsaSigningKeyStore keys) : IJwksProvider
{
    public string GetJwksJson()
    {
        var jwkList = keys.PublicKeys.Select(key =>
        {
            var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
            return new
            {
                kty = jwk.Kty,
                use = "sig",
                alg = "RS256",
                kid = jwk.Kid,
                n = jwk.N,
                e = jwk.E,
            };
        });

        return JsonSerializer.Serialize(new { keys = jwkList });
    }
}
