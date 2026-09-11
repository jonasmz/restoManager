using FluentValidation;
using RestoManager.Auth.Domain.Auth;
using RestoManager.Auth.Domain.Users;

namespace RestoManager.Auth.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string Password,
    string Role,
    int EmployeeId,
    IReadOnlyList<int> BranchIds);

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator(IEmployeeDirectoryClient employeeDirectory)
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role)
            .Must(AuthRoles.IsValid)
            .WithMessage(x => $"Rol inválido '{x.Role}'. Válidos: {string.Join(", ", AuthRoles.All)}.");
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.EmployeeId)
            .MustAsync((employeeId, ct) => employeeDirectory.ExistsAsync(employeeId, ct))
            .WithMessage(x => $"No existe un empleado con id {x.EmployeeId} en el negocio.")
            .When(x => x.EmployeeId > 0);
        RuleFor(x => x.BranchIds).NotEmpty();
        RuleForEach(x => x.BranchIds).GreaterThan(0);
    }
}
