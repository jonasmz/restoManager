using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace RestoManager.Auth.Infrastructure.Tokens;

/// <summary>
/// Carga (o genera en desarrollo) el par de claves RSA de firma. Publica la clave
/// privada actual para firmar y las públicas activas (actual + siguiente) para
/// validar y para el JWKS. Formato de fichero: PEM PKCS#8.
/// </summary>
public sealed class RsaSigningKeyStore
{
    public SigningCredentials SigningCredentials { get; }

    /// <summary>Claves públicas activas (solo parámetros públicos), para validación y JWKS.</summary>
    public IReadOnlyList<RsaSecurityKey> PublicKeys { get; }

    public RsaSigningKeyStore(string keysDirectory)
    {
        Directory.CreateDirectory(keysDirectory);

        var currentPath = Path.Combine(keysDirectory, "current.pem");
        var nextPath = Path.Combine(keysDirectory, "next.pem");

        var current = LoadOrCreate(currentPath);
        SigningCredentials = new SigningCredentials(
            ToPrivateKey(current, "current"), "RS256");

        var publicKeys = new List<RsaSecurityKey> { ToPublicKey(current, "current") };
        if (File.Exists(nextPath))
        {
            using var next = LoadExisting(nextPath);
            publicKeys.Add(ToPublicKey(next, "next"));
        }

        PublicKeys = publicKeys;
    }

    private static RSA LoadOrCreate(string path)
    {
        if (File.Exists(path))
        {
            return LoadExisting(path);
        }

        var rsa = RSA.Create(2048);
        File.WriteAllText(path, rsa.ExportPkcs8PrivateKeyPem());
        return rsa;
    }

    private static RSA LoadExisting(string path)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(path));
        return rsa;
    }

    private static RsaSecurityKey ToPrivateKey(RSA rsa, string label)
        => new(rsa) { KeyId = ComputeKid(rsa, label) };

    private static RsaSecurityKey ToPublicKey(RSA rsa, string label)
    {
        var publicOnly = RSA.Create();
        publicOnly.ImportParameters(rsa.ExportParameters(includePrivateParameters: false));
        return new RsaSecurityKey(publicOnly) { KeyId = ComputeKid(rsa, label) };
    }

    private static string ComputeKid(RSA rsa, string label)
    {
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var digest = SHA256.HashData(spki);
        return Convert.ToBase64String(digest, 0, 12)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_') + "-" + label;
    }
}
