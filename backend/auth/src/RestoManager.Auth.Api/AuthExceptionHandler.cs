using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RestoManager.Auth.Domain.Auth;

namespace RestoManager.Auth.Api;

/// <summary>Traduce las excepciones esperables del dominio a <c>ProblemDetails</c>.</summary>
public sealed class AuthExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, type, extensions) = exception switch
        {
            ValidationException v => (
                StatusCodes.Status422UnprocessableEntity,
                "La solicitud no es válida.",
                "validation",
                (IDictionary<string, object?>)new Dictionary<string, object?>
                {
                    ["errors"] = v.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
                }),
            RefreshTokenReuseException e => (
                StatusCodes.Status401Unauthorized, e.Message, e.Code, EmptyExtensions()),
            InvalidCredentialsException e => (
                StatusCodes.Status401Unauthorized, e.Message, e.Code, EmptyExtensions()),
            InvalidRefreshTokenException e => (
                StatusCodes.Status401Unauthorized, e.Message, e.Code, EmptyExtensions()),
            UserCreationException e => (
                StatusCodes.Status409Conflict, e.Message, e.Code, EmptyExtensions()),
            _ => (0, string.Empty, string.Empty, EmptyExtensions()),
        };

        if (status == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = $"https://resto-manager/errors/{type}",
                Extensions = extensions,
            },
        });
    }

    private static IDictionary<string, object?> EmptyExtensions() => new Dictionary<string, object?>();
}
