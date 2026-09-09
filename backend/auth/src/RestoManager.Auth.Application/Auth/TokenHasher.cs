using System.Security.Cryptography;
using System.Text;

namespace RestoManager.Auth.Application.Auth;

/// <summary>Genera el valor en claro del refresh token y su hash de almacenamiento.</summary>
public static class TokenHasher
{
    /// <summary>Nuevo secreto aleatorio de 256 bits, en Base64Url.</summary>
    public static string NewSecret()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>SHA-256 (Base64) del valor. Solo el hash se persiste.</summary>
    public static string Hash(string value)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToBase64String(digest);
    }
}
