namespace RestoManager.Auth.Domain.Tokens;

/// <summary>Expone las claves públicas activas como JWKS (para <c>/.well-known/jwks.json</c>).</summary>
public interface IJwksProvider
{
    /// <summary>JSON del JWKS con todas las claves públicas activas (RS256).</summary>
    string GetJwksJson();
}
