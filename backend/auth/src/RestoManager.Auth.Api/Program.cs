using RestoManager.Auth.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddAuthInfrastructure(
    builder.Configuration.GetConnectionString("IdentityDb"));

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

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "RestoManager.Auth.Api", status = "ok" }));

app.Run();

// Punto de extensión para pruebas de integración (WebApplicationFactory<Program>).
public partial class Program;
