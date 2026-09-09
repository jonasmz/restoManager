namespace RestoManager.Auth.Application.Auth.Logout;

public sealed class LogoutHandler(TokenService tokens)
{
    public Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return Task.CompletedTask;
        }

        return tokens.RevokeAsync(command.RefreshToken, cancellationToken);
    }
}
