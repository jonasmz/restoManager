namespace RestoManager.Auth.Domain.Users;

/// <summary>Datos para dar de alta un usuario (solo lo hace un administrador).</summary>
public sealed record CreateUserRequest(
    string Email,
    string Password,
    string Role,
    int EmployeeId,
    IReadOnlyList<int> BranchIds);
