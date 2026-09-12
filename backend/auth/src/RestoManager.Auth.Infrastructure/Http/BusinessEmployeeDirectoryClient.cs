using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RestoManager.Auth.Domain.Users;

namespace RestoManager.Auth.Infrastructure.Http;

/// <summary>Implementación HTTP de <see cref="IEmployeeDirectoryClient"/> contra los endpoints
/// internos de la Business API (ver Business.Api/Endpoints/InternalEndpoints.cs). La base
/// address y el header X-Internal-Key se configuran una sola vez al registrar el HttpClient
/// tipado (ver DependencyInjection.cs).</summary>
public sealed class BusinessEmployeeDirectoryClient(HttpClient http, ILogger<BusinessEmployeeDirectoryClient> logger)
    : IEmployeeDirectoryClient
{
    public async Task<bool> ExistsAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await http.GetAsync($"/internal/employees/{employeeId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var body = await response.Content.ReadFromJsonAsync<ExistsResponse>(cancellationToken);
            return body?.Exists ?? false;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "No se pudo validar el empleado {EmployeeId} contra Business.", employeeId);
            return false;
        }
    }

    public async Task<bool> LinkUserAsync(int employeeId, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await http.PutAsJsonAsync(
                $"/internal/employees/{employeeId}/user-id", new { userId }, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "No se pudo vincular el empleado {EmployeeId} con el usuario {UserId} en Business.", employeeId, userId);
            return false;
        }
    }

    private sealed record ExistsResponse(bool Exists);
}
