using RestoManager.Business.Application.Abstractions;

namespace RestoManager.Business.Api.Auth;

/// <summary>Resuelve <see cref="ICurrentUser"/> desde el <see cref="HttpContext"/> de la petición.</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ICurrentUser Inner => CurrentUserClaims.FromPrincipal(accessor.HttpContext?.User);

    public bool IsAuthenticated => Inner.IsAuthenticated;
    public int UserId => Inner.UserId;
    public string Email => Inner.Email;
    public IReadOnlyList<string> Roles => Inner.Roles;
    public int EmployeeId => Inner.EmployeeId;
    public IReadOnlyList<int> BranchIds => Inner.BranchIds;

    public bool IsInRole(string role) => Inner.IsInRole(role);
    public bool CanOperateInBranch(int branchId) => Inner.CanOperateInBranch(branchId);
}
