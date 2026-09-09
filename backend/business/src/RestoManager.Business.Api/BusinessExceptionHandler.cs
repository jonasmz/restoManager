using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RestoManager.Business.Domain.Common;

namespace RestoManager.Business.Api;

/// <summary>Traduce las excepciones esperables del dominio a <c>ProblemDetails</c>.</summary>
public sealed class BusinessExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
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
            BusinessException b => (
                b.StatusCode, b.Message, b.Code, new Dictionary<string, object?>()),
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest, "Solicitud mal formada.", "bad_request", new Dictionary<string, object?>()),
            _ => (0, string.Empty, string.Empty, new Dictionary<string, object?>()),
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
}
