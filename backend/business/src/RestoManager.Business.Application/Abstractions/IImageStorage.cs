namespace RestoManager.Business.Application.Abstractions;

/// <summary>
/// Puerto de salida para guardar imágenes ilustrativas de la carta (Fase 11). El
/// adaptador por defecto escribe en el sistema de archivos (un volumen del contenedor);
/// la validación de tipo y tamaño vive en la capa de aplicación.
/// </summary>
public interface IImageStorage
{
    /// <summary>Guarda el contenido y devuelve su clave pública (nombre de archivo).</summary>
    Task<string> SaveAsync(Stream content, string contentType, CancellationToken ct = default);

    /// <summary>Borra la imagen identificada por <paramref name="key"/>. No falla si no existe.</summary>
    void Delete(string key);
}
