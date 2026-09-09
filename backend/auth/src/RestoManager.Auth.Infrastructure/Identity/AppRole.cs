using Microsoft.AspNetCore.Identity;

namespace RestoManager.Auth.Infrastructure.Identity;

public sealed class AppRole : IdentityRole<int>
{
    public AppRole() { }

    public AppRole(string name) : base(name) { }
}
