using RestoManager.Auth.Api.Contracts;
using RestoManager.Auth.Application.Auth;
using RestoManager.Auth.Application.Auth.Login;
using RestoManager.Auth.Application.Auth.Logout;
using RestoManager.Auth.Application.Auth.Refresh;
using RestoManager.Auth.Application.Users.CreateUser;
using RestoManager.Auth.Domain.Auth;

namespace RestoManager.Auth.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest body, LoginHandler handler, CancellationToken ct) =>
        {
            var pair = await handler.HandleAsync(new LoginCommand(body.Email, body.Password), ct);
            return Results.Ok(ToResponse(pair));
        });

        group.MapPost("/refresh", async (RefreshRequest body, RefreshHandler handler, CancellationToken ct) =>
        {
            var pair = await handler.HandleAsync(new RefreshCommand(body.RefreshToken), ct);
            return Results.Ok(ToResponse(pair));
        });

        group.MapPost("/logout", async (LogoutRequest body, LogoutHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new LogoutCommand(body.RefreshToken), ct);
            return Results.NoContent();
        });

        group.MapGet("/roles", () => Results.Ok(AuthRoles.All))
            .RequireAuthorization("RequireAdmin");

        group.MapPost("/users", async (CreateUserRequest body, CreateUserHandler handler, CancellationToken ct) =>
        {
            var id = await handler.HandleAsync(
                new CreateUserCommand(body.Email, body.Password, body.Role, body.EmployeeId, body.BranchIds), ct);
            return Results.Created($"/auth/users/{id}", new CreatedUserResponse(id));
        }).RequireAuthorization("RequireAdmin");
    }

    private static TokenResponse ToResponse(TokenPair pair) =>
        new(pair.AccessToken, pair.ExpiresInSeconds, pair.RefreshToken);
}
