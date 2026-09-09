namespace RestoManager.Auth.Domain.Tokens;

/// <summary>Emite access tokens JWT firmados con la clave RSA activa.</summary>
public interface ITokenIssuer
{
    AccessToken Issue(AccessTokenClaims claims);
}
