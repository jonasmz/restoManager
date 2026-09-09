namespace RestoManager.Auth.Domain.Auth;

/// <summary>
/// Catálogo fijo de roles (decisión de Fase 1). Sin CRUD de roles en esta versión.
/// </summary>
public static class AuthRoles
{
    public const string Admin = "ADMIN";
    public const string BranchManager = "BRANCH_MANAGER";
    public const string Waiter = "WAITER";
    public const string Kitchen = "KITCHEN";
    public const string Inventory = "INVENTORY";

    public static readonly IReadOnlyList<string> All =
    [
        Admin, BranchManager, Waiter, Kitchen, Inventory,
    ];

    public static bool IsValid(string role) => All.Contains(role);
}
