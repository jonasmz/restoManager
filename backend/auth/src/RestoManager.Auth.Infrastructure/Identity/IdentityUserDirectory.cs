using System.Globalization;
using Microsoft.AspNetCore.Identity;
using RestoManager.Auth.Domain.Auth;
using RestoManager.Auth.Domain.Users;

namespace RestoManager.Auth.Infrastructure.Identity;

/// <summary>Adaptador de <see cref="IUserDirectory"/> sobre ASP.NET Core Identity.</summary>
public sealed class IdentityUserDirectory(
    UserManager<AppUser> userManager) : IUserDirectory
{
    public async Task<UserAccount?> ValidateCredentialsAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return null;
        }

        if (await userManager.GetAccessFailedCountAsync(user) > 0)
        {
            await userManager.ResetAccessFailedCountAsync(user);
        }

        return await ToAccountAsync(user);
    }

    public async Task<UserAccount?> FindByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture));
        return user is null ? null : await ToAccountAsync(user);
    }

    public async Task<int> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            EmployeeId = request.EmployeeId,
            BranchIdsCsv = string.Join(',', request.BranchIds),
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            throw new UserCreationException(string.Join("; ", created.Errors.Select(e => e.Description)));
        }

        var roleAssigned = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleAssigned.Succeeded)
        {
            throw new UserCreationException(string.Join("; ", roleAssigned.Errors.Select(e => e.Description)));
        }

        return user.Id;
    }

    private async Task<UserAccount> ToAccountAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserAccount(
            user.Id,
            user.Email ?? user.UserName ?? string.Empty,
            roles.ToList(),
            user.EmployeeId,
            ParseBranchIds(user.BranchIdsCsv));
    }

    private static IReadOnlyList<int> ParseBranchIds(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        var ids = new List<int>();
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }
}
