namespace RestoManager.Auth.Domain.Users;

/// <summary>
/// Puerto hacia la Business API (empleados), usado solo al dar de alta un usuario: valida que
/// el <c>EmployeeId</c> recibido corresponda a un empleado real antes de crear la cuenta — antes
/// se aceptaba cualquier entero sin verificar. Implementado en Infrastructure con una llamada
/// HTTP interna servicio-a-servicio (ver deploy/nginx/, que no expone esta ruta).
/// </summary>
public interface IEmployeeDirectoryClient
{
    /// <summary>
    /// <c>true</c> si el empleado existe en Business. Ante cualquier error de red o timeout
    /// devuelve <c>false</c> (fail-closed): nunca se crea un login sin poder confirmar el vínculo.
    /// </summary>
    Task<bool> ExistsAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Avisa a Business que <paramref name="employeeId"/> quedó vinculado a
    /// <paramref name="userId"/> (usado por el seed del admin de arranque; el alta interactiva
    /// desde el panel hace este mismo llamado ella misma, autenticada con su propio JWT).
    /// Devuelve <c>false</c> ante cualquier error — nunca lanza.
    /// </summary>
    Task<bool> LinkUserAsync(int employeeId, int userId, CancellationToken cancellationToken = default);
}
