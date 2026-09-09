namespace RestoManager.Auth.Domain.Auth;

/// <summary>Errores esperables del flujo de autenticación. La capa Api los mapea a <c>ProblemDetails</c>.</summary>
public abstract class AuthException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class InvalidCredentialsException()
    : AuthException("auth.invalid_credentials", "Correo o contraseña inválidos.");

public sealed class InvalidRefreshTokenException()
    : AuthException("auth.invalid_refresh_token", "El refresh token no es válido o expiró.");

/// <summary>Se presentó un refresh token ya consumido o revocado: se revoca toda la sesión.</summary>
public sealed class RefreshTokenReuseException()
    : AuthException("auth.refresh_token_reuse", "Se detectó reutilización de un refresh token; la sesión fue revocada.");

public sealed class UserCreationException(string message)
    : AuthException("auth.user_creation_failed", message);
