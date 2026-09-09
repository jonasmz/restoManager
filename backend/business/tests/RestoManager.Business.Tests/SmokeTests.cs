using System.Security.Claims;
using RestoManager.Business.Api.Auth;

namespace RestoManager.Business.Tests;

public class CurrentUserClaimsTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "TestJwt"));

    [Fact]
    public void Unauthenticated_principal_maps_to_anonymous()
    {
        var user = CurrentUserClaims.FromPrincipal(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(user.IsAuthenticated);
        Assert.Empty(user.Roles);
        Assert.Empty(user.BranchIds);
    }

    [Fact]
    public void Null_principal_maps_to_anonymous()
        => Assert.False(CurrentUserClaims.FromPrincipal(null).IsAuthenticated);

    [Fact]
    public void Reads_scalar_branch_id_claim()
    {
        var user = CurrentUserClaims.FromPrincipal(Principal(
            new Claim("sub", "1"),
            new Claim("email", "admin@resto.local"),
            new Claim("employee_id", "1"),
            new Claim("role", "ADMIN"),
            new Claim("branch_id", "1")));

        Assert.True(user.IsAuthenticated);
        Assert.Equal(1, user.UserId);
        Assert.Equal("admin@resto.local", user.Email);
        Assert.Equal(1, user.EmployeeId);
        Assert.Equal(["ADMIN"], user.Roles);
        Assert.Equal([1], user.BranchIds);
        Assert.True(user.CanOperateInBranch(1));
        Assert.False(user.CanOperateInBranch(2));
    }

    [Fact]
    public void Reads_multiple_branch_id_and_role_claims()
    {
        var user = CurrentUserClaims.FromPrincipal(Principal(
            new Claim("sub", "2"),
            new Claim("employee_id", "5"),
            new Claim("role", "WAITER"),
            new Claim("role", "KITCHEN"),
            new Claim("branch_id", "1"),
            new Claim("branch_id", "2"),
            new Claim("branch_id", "2")));

        Assert.Equal(["WAITER", "KITCHEN"], user.Roles);
        Assert.Equal([1, 2], user.BranchIds);
        Assert.True(user.IsInRole("KITCHEN"));
    }

    [Fact]
    public void Ignores_non_numeric_branch_id()
    {
        var user = CurrentUserClaims.FromPrincipal(Principal(
            new Claim("sub", "3"),
            new Claim("branch_id", "abc"),
            new Claim("branch_id", "4")));

        Assert.Equal([4], user.BranchIds);
    }
}
