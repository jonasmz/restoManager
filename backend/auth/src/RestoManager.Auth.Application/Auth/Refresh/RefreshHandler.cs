using FluentValidation;

namespace RestoManager.Auth.Application.Auth.Refresh;

public sealed class RefreshHandler(TokenService tokens, IValidator<RefreshCommand> validator)
{
    public async Task<TokenPair> HandleAsync(RefreshCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        return await tokens.RotateAsync(command.RefreshToken, cancellationToken);
    }
}
