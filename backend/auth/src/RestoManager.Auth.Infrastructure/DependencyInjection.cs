using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestoManager.Auth.Application.Auth;
using RestoManager.Auth.Domain.Abstractions;
using RestoManager.Auth.Domain.RefreshTokens;
using RestoManager.Auth.Domain.Tokens;
using RestoManager.Auth.Domain.Users;
using RestoManager.Auth.Infrastructure.Http;
using RestoManager.Auth.Infrastructure.Identity;
using RestoManager.Auth.Infrastructure.Persistence;
using RestoManager.Auth.Infrastructure.RefreshTokens;
using RestoManager.Auth.Infrastructure.Setup;
using RestoManager.Auth.Infrastructure.Time;
using RestoManager.Auth.Infrastructure.Tokens;

namespace RestoManager.Auth.Infrastructure;

/// <summary>Adaptadores de salida de la Auth API: persistencia (Identity), firma RSA, JWKS.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAuthInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();
        services.AddSingleton(authOptions);
        services.AddSingleton(new AuthTokenOptions
        {
            AccessTokenLifetime = TimeSpan.FromMinutes(authOptions.AccessTokenMinutes),
            RefreshTokenLifetime = TimeSpan.FromDays(authOptions.RefreshTokenDays),
        });

        services.AddDbContext<AuthDbContext>(o =>
            o.UseNpgsql(configuration.GetConnectionString("IdentityDb")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserDirectory, IdentityUserDirectory>();
        services.AddScoped<IdentityDataSeeder>();

        var businessOptions = configuration.GetSection(BusinessApiOptions.SectionName).Get<BusinessApiOptions>()
            ?? new BusinessApiOptions();
        services.AddSingleton(businessOptions);
        services.AddHttpClient<IEmployeeDirectoryClient, BusinessEmployeeDirectoryClient>(client =>
        {
            client.BaseAddress = new Uri(businessOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
            if (!string.IsNullOrEmpty(businessOptions.InternalApiKey))
            {
                client.DefaultRequestHeaders.Add("X-Internal-Key", businessOptions.InternalApiKey);
            }
        });

        services
            .AddIdentityCore<AppUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.Lockout.MaxFailedAccessAttempts = 10;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AuthDbContext>();

        var keysDir = Path.IsPathRooted(authOptions.SigningKeysDirectory)
            ? authOptions.SigningKeysDirectory
            : Path.Combine(contentRootPath, authOptions.SigningKeysDirectory);

        services.AddSingleton(new RsaSigningKeyStore(keysDir));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ITokenIssuer, RsaTokenIssuer>();
        services.AddSingleton<IJwksProvider, JwksProvider>();

        return services;
    }
}
