using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using RestoManager.Business.Api;
using RestoManager.Business.Api.Auth;
using RestoManager.Business.Api.Endpoints;
using RestoManager.Business.Api.Reports;
using RestoManager.Business.Application.Reports;
using RestoManager.Business.Application;
using RestoManager.Business.Application.Abstractions;
using RestoManager.Business.Infrastructure;
using RestoManager.Business.Infrastructure.Persistence;

QuestPDF.Settings.License = LicenseType.Community; // Fase 10: PDF de reportes.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
builder.Services.AddScoped<IBranchContext, HeaderBranchContext>();
builder.Services.AddExceptionHandler<BusinessExceptionHandler>();

builder.Services.AddBusinessApplication();
builder.Services.AddBusinessInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IReportRenderer, QuestPdfReportRenderer>();

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
        .AddPolicy("RequireAdmin", policy => policy.RequireRole("ADMIN"))
        .AddPolicy("InventoryAccess", policy => policy.RequireRole("ADMIN", "BRANCH_MANAGER", "INVENTORY"))
        .AddPolicy("OrgAdmin", policy => policy.RequireRole("ADMIN"))
        .AddPolicy("OrgStaff", policy => policy.RequireRole("ADMIN", "BRANCH_MANAGER"))
        .AddPolicy("MenuAccess", policy => policy.RequireRole("ADMIN", "BRANCH_MANAGER"))
        .AddPolicy("DiningRoomAccess", policy => policy.RequireRole("ADMIN", "BRANCH_MANAGER", "WAITER"))
        .AddPolicy("SalesAccess", policy => policy.RequireRole("ADMIN", "BRANCH_MANAGER", "WAITER"))
        .AddPolicy("DiscountAccess", policy => policy.RequireRole("ADMIN", "BRANCH_MANAGER"))
        .AddPolicy("DeliveryAccess", policy => policy.RequireRole("ADMIN", "BRANCH_MANAGER", "WAITER"))
        .AddPolicy("ReportsAccess", policy => policy.RequireRole("ADMIN", "BRANCH_MANAGER"));
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
    app.MapOrganizationEndpoints();
    app.MapInventoryEndpoints();
    app.MapPurchasingEndpoints();
    app.MapMenuEndpoints();
    app.MapDiningRoomEndpoints();
    app.MapSalesEndpoints();
    app.MapDeliveryEndpoints();
    app.MapCustomerEndpoints();
    app.MapReportEndpoints();
    app.MapReportPdfEndpoints();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BusinessDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await scope.ServiceProvider
            .GetRequiredService<RestoManager.Business.Infrastructure.Setup.BusinessDevSeeder>()
            .SeedAsync();
    }
}

app.Run();

// Punto de extensión para pruebas de integración (WebApplicationFactory<Program>).
public partial class Program;
