using FluentValidation;
using RestoManager.Auth.Domain.Auth;
using RestoManager.Auth.Domain.Users;

namespace RestoManager.Auth.Application.Auth.Login;

public sealed class LoginHandler(
    IUserDirectory users,
    TokenService tokens,
    IValidator<LoginCommand> validator)
{
    public async Task<TokenPair> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var user = await users.ValidateCredentialsAsync(command.Email.Trim(), command.Password, cancellationToken)
            ?? throw new InvalidCredentialsException();

        return await tokens.StartSessionAsync(user, cancellationToken);
    }
}
