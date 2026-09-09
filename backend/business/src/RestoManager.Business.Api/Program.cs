using Microsoft.AspNetCore.Authentication.JwtBearer;
using RestoManager.Business.Api.Auth;
using RestoManager.Business.Api.Endpoints;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddBusinessInfrastructure(
    builder.Configuration.GetConnectionString("BusinessDb"));

// Validación del JWT emitido por la Auth API (descubrimiento OIDC + JWKS).
var authority = builder.Configuration["Auth:Authority"];
var authEnabled = !string.IsNullOrWhiteSpace(authority);
if (authEnabled)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.Audience = builder.Configuration["Auth:Audience"];
            options.RequireHttpsMetadata = builder.Environment.IsProduction();
            options.MapInboundClaims = false;
            options.TokenValidationParameters.NameClaimType = "sub";
            options.TokenValidationParameters.RoleClaimType = "role";
        });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy("RequireAdmin", policy => policy.RequireRole("ADMIN"));
}

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

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "RestoManager.Business.Api", status = "ok" }));

if (authEnabled)
{
    app.MapMeEndpoints();
}

app.Run();

// Punto de extensión para pruebas de integración (WebApplicationFactory<Program>).
public partial class Program;
