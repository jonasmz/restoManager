using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestoManager.Auth.Api;
using RestoManager.Auth.Api.Endpoints;
using RestoManager.Auth.Application;
using RestoManager.Auth.Infrastructure;
using RestoManager.Auth.Infrastructure.Persistence;
using RestoManager.Auth.Infrastructure.Setup;
using RestoManager.Auth.Infrastructure.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<AuthExceptionHandler>();

builder.Services.AddAuthApplication();
builder.Services.AddAuthInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);

var authOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

// La Auth API valida sus propios tokens para proteger la gestión de usuarios.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<RsaSigningKeyStore>((options, keys) =>
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = keys.PublicKeys,
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireAdmin", policy => policy.RequireRole("ADMIN"));

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "RestoManager.Auth.Api", status = "ok" }));
app.MapAuthEndpoints();
app.MapWellKnownEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<IdentityDataSeeder>().SeedAsync();
}

app.Run();

// Punto de extensión para pruebas de integración (WebApplicationFactory<Program>).
public partial class Program;
