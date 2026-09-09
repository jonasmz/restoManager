using RestoManager.Auth.Domain.Users;

namespace RestoManager.Auth.Domain.Tokens;

/// <summary>Datos que van dentro del access token.</summary>
public sealed record AccessTokenClaims(
    int UserId,
    string Email,
    IReadOnlyList<string> Roles,
    int EmployeeId,
    IReadOnlyList<int> BranchIds)
{
    public static AccessTokenClaims FromUser(UserAccount user) =>
        new(user.Id, user.Email, user.Roles, user.EmployeeId, user.BranchIds);
}
