namespace RestoManager.Business.Application.Abstractions;

/// <summary>
/// Sucursal activa de la petición. La resuelve la capa Api desde el header
/// <c>X-Branch-Id</c>, validada contra los claims del token (regla transversal §1).
/// Si el usuario opera en una sola sucursal y no manda el header, se asume esa.
/// </summary>
public interface IBranchContext
{
    bool HasBranch { get; }

    /// <summary>Id de la sucursal activa. Lanza si no se pudo determinar.</summary>
    int BranchId { get; }
}
