using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RestoManager.Auth.Application.Auth;
using RestoManager.Auth.Application.Auth.Login;
using RestoManager.Auth.Application.Auth.Logout;
using RestoManager.Auth.Application.Auth.Refresh;
using RestoManager.Auth.Application.Users.CreateUser;

namespace RestoManager.Auth.Application;

/// <summary>Registra los casos de uso de la Auth API y sus validadores.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuthApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<LoginCommand>, LoginValidator>();
        services.AddScoped<IValidator<RefreshCommand>, RefreshValidator>();
        services.AddScoped<IValidator<CreateUserCommand>, CreateUserValidator>();

        services.AddScoped<TokenService>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<CreateUserHandler>();

        return services;
    }
}
