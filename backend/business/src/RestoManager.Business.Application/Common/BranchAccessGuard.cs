using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Application.Common;

/// <summary>
/// DOM-06: valida que el usuario del token esté habilitado para operar en la
/// sucursal indicada. `ADMIN` puede operar en cualquiera.
/// </summary>
public sealed class BranchAccessGuard(ICurrentUser currentUser)
{
    public void EnsureCanOperate(int branchId)
    {
        if (currentUser.IsInRole("ADMIN"))
        {
            return;
        }
        if (!currentUser.CanOperateInBranch(branchId))
        {
            throw new BranchAccessDeniedException(branchId);
        }
    }
}
