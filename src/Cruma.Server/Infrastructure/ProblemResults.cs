using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cruma.Server.Infrastructure;

/// <summary>Převod výsledků aplikačních služeb na HTTP odpovědi s ProblemDetails (ERR-001, API-002).</summary>
public static class ProblemResults
{
    public static ProblemHttpResult Problem(ServiceError error) =>
        TypedResults.Problem(
            statusCode: ErrorCodes.StatusFor(error.Code),
            detail: error.Detail,
            extensions: new Dictionary<string, object?>(error.Extensions ?? new Dictionary<string, object?>())
            {
                ["code"] = error.Code,
            });

    public static IResult ToHttp<T>(this ServiceResult<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : Problem(result.Error!);

    public static IResult ToHttp(this ServiceResult result, Func<IResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : Problem(result.Error!);

    /// <summary>
    /// Doplní do každé ProblemDetails kód podle stavu (pokud chybí) a correlation id. Neošetřená výjimka dostane
    /// obecný popis bez detailů (ERR-001).
    /// </summary>
    public static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var status = problem.Status ?? context.HttpContext.Response.StatusCode;
        problem.Extensions.TryAdd("code", ErrorCodes.ForStatus(status));

        var requestContext = context.HttpContext.RequestServices.GetService<RequestContext>();
        if (!string.IsNullOrEmpty(requestContext?.CorrelationId))
        {
            problem.Extensions["correlationId"] = requestContext.CorrelationId;
        }

        if (status >= StatusCodes.Status500InternalServerError)
        {
            problem.Detail = null;
            problem.Extensions.Remove("exception");
        }
    }
}
