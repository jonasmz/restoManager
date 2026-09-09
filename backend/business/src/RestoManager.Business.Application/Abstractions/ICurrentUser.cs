namespace RestoManager.Business.Application.Abstractions;

/// <summary>
/// Usuario autenticado de la petición en curso, tomado de los claims del JWT emitido
/// por la Auth API. Los casos de uso lo usan para filtrar por sucursal y validar
/// <c>DOM-06</c> (empleado habilitado para la sucursal).
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int UserId { get; }
    string Email { get; }
    IReadOnlyList<string> Roles { get; }
    int EmployeeId { get; }

    /// <summary>Sucursales del usuario (claim <c>branch_id</c>, escalar o lista).</summary>
    IReadOnlyList<int> BranchIds { get; }

    bool IsInRole(string role);
    bool CanOperateInBranch(int branchId);
}
