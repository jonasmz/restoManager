using FluentValidation;

namespace RestoManager.Auth.Application.Auth.Refresh;

public sealed record RefreshCommand(string RefreshToken);

public sealed class RefreshValidator : AbstractValidator<RefreshCommand>
{
    public RefreshValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}
