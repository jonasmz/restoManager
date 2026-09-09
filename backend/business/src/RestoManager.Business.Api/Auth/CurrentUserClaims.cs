using System.Globalization;
using System.Security.Claims;
using RestoManager.Business.Application.Abstractions;

namespace RestoManager.Business.Api.Auth;

/// <summary>
/// Lee los claims del JWT (emitidos por la Auth API) en una estructura tipada.
/// <c>branch_id</c> puede venir como escalar (una sucursal) o como varios claims
/// (varias sucursales): <see cref="ClaimsPrincipal.FindAll(string)"/> cubre ambos.
/// </summary>
public sealed class CurrentUserClaims : ICurrentUser
{
    private CurrentUserClaims(
        bool isAuthenticated, int userId, string email,
        IReadOnlyList<string> roles, int employeeId, IReadOnlyList<int> branchIds)
    {
        IsAuthenticated = isAuthenticated;
        UserId = userId;
        Email = email;
        Roles = roles;
        EmployeeId = employeeId;
        BranchIds = branchIds;
    }

    public bool IsAuthenticated { get; }
    public int UserId { get; }
    public string Email { get; }
    public IReadOnlyList<string> Roles { get; }
    public int EmployeeId { get; }
    public IReadOnlyList<int> BranchIds { get; }

    public bool IsInRole(string role) => Roles.Contains(role);

    public bool CanOperateInBranch(int branchId) => BranchIds.Contains(branchId);

    public static readonly CurrentUserClaims Anonymous =
        new(false, 0, string.Empty, [], 0, []);

    public static CurrentUserClaims FromPrincipal(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is not { IsAuthenticated: true })
        {
            return Anonymous;
        }

        return new CurrentUserClaims(
            isAuthenticated: true,
            userId: ParseInt(principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)),
            email: principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            roles: principal.FindAll("role").Concat(principal.FindAll(ClaimTypes.Role))
                .Select(c => c.Value).Distinct().ToArray(),
            employeeId: ParseInt(principal.FindFirstValue("employee_id")),
            branchIds: principal.FindAll("branch_id")
                .Select(c => ParseInt(c.Value)).Where(id => id > 0).Distinct().ToArray());
    }

    private static int ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
}
