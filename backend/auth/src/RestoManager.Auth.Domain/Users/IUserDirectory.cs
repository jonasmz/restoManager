namespace RestoManager.Auth.Domain.Users;

/// <summary>
/// Puerto sobre el almacén de identidad (implementado con ASP.NET Core Identity).
/// </summary>
public interface IUserDirectory
{
    /// <summary>Devuelve el usuario si el correo existe y la contraseña es correcta; si no, <c>null</c>.</summary>
    Task<UserAccount?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<UserAccount?> FindByIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Crea el usuario y le asigna el rol. Lanza <see cref="Auth.UserCreationException"/> si falla.</summary>
    Task<int> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
}
