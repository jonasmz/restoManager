namespace RestoManager.Auth.Api.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string Role,
    int EmployeeId,
    int[] BranchIds);

public sealed record TokenResponse(string AccessToken, int ExpiresIn, string RefreshToken, string TokenType = "Bearer");

public sealed record CreatedUserResponse(int Id);
