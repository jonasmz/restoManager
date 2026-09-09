using FluentValidation;
using RestoManager.Auth.Domain.Users;

namespace RestoManager.Auth.Application.Users.CreateUser;

public sealed class CreateUserHandler(IUserDirectory users, IValidator<CreateUserCommand> validator)
{
    public async Task<int> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        return await users.CreateAsync(
            new CreateUserRequest(
                command.Email.Trim(),
                command.Password,
                command.Role,
                command.EmployeeId,
                command.BranchIds),
            cancellationToken);
    }
}
