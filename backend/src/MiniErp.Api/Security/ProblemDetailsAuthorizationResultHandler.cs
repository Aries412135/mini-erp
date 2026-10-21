using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace MiniErp.Api.Security;

public class ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();
    private readonly IProblemDetailsService _problemDetails;

    public ProblemDetailsAuthorizationResultHandler(IProblemDetailsService problemDetails)
    {
        _problemDetails = problemDetails;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizationResult)
    {
        await _default.HandleAsync(next, context, policy, authorizationResult);

        if (authorizationResult.Succeeded || context.Response.HasStarted)
        {
            return;
        }

        var problem = authorizationResult.Challenged
            ? new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Autentikasi diperlukan.",
                Detail = "Sertakan access token yang valid."
            }
            : new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Akses ditolak.",
                Detail = "Akun Anda tidak memiliki hak untuk aksi ini."
            };

        await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem
        });
    }
}