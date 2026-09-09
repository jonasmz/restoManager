using RestoManager.Auth.Domain.Tokens;
using RestoManager.Auth.Infrastructure.Setup;

namespace RestoManager.Auth.Api.Endpoints;

public static class WellKnownEndpoints
{
    public static void MapWellKnownEndpoints(this WebApplication app)
    {
        app.MapGet("/.well-known/jwks.json", (IJwksProvider jwks) =>
            Results.Content(jwks.GetJwksJson(), "application/json")).WithTags("Discovery");

        app.MapGet("/.well-known/openid-configuration", (AuthOptions options) =>
        {
            var issuer = options.Issuer.TrimEnd('/');
            return Results.Ok(new
            {
                issuer,
                jwks_uri = $"{issuer}/.well-known/jwks.json",
                id_token_signing_alg_values_supported = new[] { "RS256" },
                token_endpoint_auth_methods_supported = new[] { "none" },
                grant_types_supported = new[] { "password", "refresh_token" },
            });
        }).WithTags("Discovery");
    }
}
