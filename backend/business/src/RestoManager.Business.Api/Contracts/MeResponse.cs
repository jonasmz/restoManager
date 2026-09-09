namespace RestoManager.Business.Api.Contracts;

public sealed record MeResponse(
    int UserId,
    string Email,
    IReadOnlyList<string> Roles,
    int EmployeeId,
    IReadOnlyList<int> BranchIds);
