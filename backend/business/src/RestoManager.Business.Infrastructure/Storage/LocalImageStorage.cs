using RestoManager.Business.Application.Abstractions;

namespace RestoManager.Business.Infrastructure.Storage;

/// <summary>
/// Adaptador de <see cref="IImageStorage"/> sobre el sistema de archivos local (un volumen
/// montado en el contenedor). La API sirve estas imágenes en <c>/media/menu/&lt;clave&gt;</c>.
/// La validación de tipo y tamaño vive en la capa de aplicación; aquí solo se escribe.
/// </summary>
public sealed class LocalImageStorage : IImageStorage
{
    private static readonly Dictionary<string, string> ExtensionByType =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = "jpg",
            ["image/png"] = "png",
            ["image/webp"] = "webp",
        };

    private readonly string _root;

    public LocalImageStorage(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct = default)
    {
        var extension = ExtensionByType.GetValueOrDefault(contentType, "bin");
        var key = $"{Guid.NewGuid():N}.{extension}";
        var path = Path.Combine(_root, key);

        await using var file = File.Create(path);
        await content.CopyToAsync(file, ct);
        return key;
    }

    public void Delete(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        // Solo el nombre de archivo: evita salir del directorio raíz.
        var path = Path.Combine(_root, Path.GetFileName(key));
        try
        {
            File.Delete(path);
        }
        catch (DirectoryNotFoundException)
        {
            // Nada que borrar.
        }
    }
}
