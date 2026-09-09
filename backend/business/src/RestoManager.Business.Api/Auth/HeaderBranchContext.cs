using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Api.Auth;

/// <summary>
/// Resuelve la sucursal activa desde el header <c>X-Branch-Id</c>, validada contra
/// los claims del token. Sin header: si el usuario opera en una sola sucursal, se
/// asume esa.
/// </summary>
public sealed class HeaderBranchContext(IHttpContextAccessor accessor, ICurrentUser currentUser) : IBranchContext
{
    private const string HeaderName = "X-Branch-Id";

    public bool HasBranch => Resolve() is not null;

    public int BranchId => Resolve()
        ?? throw new DomainRuleException(
            "branch.not_selected", "No se indicó sucursal activa. Envía el header X-Branch-Id.");

    private int? Resolve()
    {
        int? fromHeader = null;
        if (accessor.HttpContext?.Request.Headers.TryGetValue(HeaderName, out var raw) == true
            && int.TryParse(raw.ToString(), out var parsed) && parsed > 0)
        {
            fromHeader = parsed;
        }

        if (fromHeader is { } branchId)
        {
            if (currentUser.IsInRole("ADMIN") || currentUser.CanOperateInBranch(branchId))
            {
                return branchId;
            }
            throw new BranchAccessDeniedException(branchId);
        }

        return currentUser.BranchIds.Count == 1 ? currentUser.BranchIds[0] : null;
    }
}
