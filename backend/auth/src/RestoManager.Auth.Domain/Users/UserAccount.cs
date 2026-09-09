namespace RestoManager.Auth.Domain.Users;

/// <summary>
/// Vista de dominio de un usuario autenticable. En Fase 1 <see cref="EmployeeId"/> y
/// <see cref="BranchIds"/> se guardan en el propio usuario; la Fase 2 los reconcilia
/// con la tabla <c>employees</c> del negocio.
/// </summary>
public sealed record UserAccount(
    int Id,
    string Email,
    IReadOnlyList<string> Roles,
    int EmployeeId,
    IReadOnlyList<int> BranchIds);
