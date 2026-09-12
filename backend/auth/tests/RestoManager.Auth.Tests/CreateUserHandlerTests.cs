using FluentValidation;
using RestoManager.Auth.Application.Users.CreateUser;
using RestoManager.Auth.Domain.Users;

namespace RestoManager.Auth.Tests;

internal sealed class FakeEmployeeDirectoryClient(bool exists) : IEmployeeDirectoryClient
{
    public Task<bool> ExistsAsync(int employeeId, CancellationToken cancellationToken = default) => Task.FromResult(exists);
    public Task<bool> LinkUserAsync(int employeeId, int userId, CancellationToken cancellationToken = default) => Task.FromResult(true);
}

internal sealed class RecordingUserDirectory : IUserDirectory
{
    public CreateUserRequest? LastRequest { get; private set; }

    public Task<UserAccount?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    public Task<UserAccount?> FindByIdAsync(int userId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
    public Task<int> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        return Task.FromResult(7);
    }
}

public class CreateUserHandlerTests
{
    private static CreateUserCommand ValidCommand() =>
        new("nueva@resto.local", "Password1", "WAITER", 1, [1]);

    private static CreateUserHandler Build(bool employeeExists, out RecordingUserDirectory users)
    {
        users = new RecordingUserDirectory();
        var validator = new CreateUserValidator(new FakeEmployeeDirectoryClient(employeeExists));
        return new CreateUserHandler(users, validator);
    }

    [Fact]
    public async Task Creates_user_when_employee_exists()
    {
        var handler = Build(employeeExists: true, out var users);

        var id = await handler.HandleAsync(ValidCommand());

        Assert.Equal(7, id);
        Assert.NotNull(users.LastRequest);
        Assert.Equal(1, users.LastRequest!.EmployeeId);
    }

    [Fact]
    public async Task Rejects_when_employee_does_not_exist_in_business()
    {
        var handler = Build(employeeExists: false, out var users);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.HandleAsync(ValidCommand()));

        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CreateUserCommand.EmployeeId));
        Assert.Null(users.LastRequest);
    }
}
