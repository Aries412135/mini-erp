// using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiniErp.Application.Common.Exceptions;

namespace MiniErp.Api.Middleware;

public class GlobalExceptionHandler: IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException ve => new HttpValidationProblemDetails(
                ve.Errors
                  .GroupBy(e => e.PropertyName)
                  .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validasi gagal."
            },

            InvalidCredentialsException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Autentikasi gagal.",
                Detail = exception.Message
            },

            InvalidRefreshTokenException => new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Autentikasi gagal.",
                Detail = exception.Message
            },
            
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Terjadi kesalahan pada server."
            }
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception pada {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            problem.GetType(),
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
        
        return true;
    }
}