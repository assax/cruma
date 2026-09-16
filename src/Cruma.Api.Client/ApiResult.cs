using System.Net;

namespace Cruma.Api.Client;

/// <summary>
/// Chyba volání API: stabilní kód z ProblemDetails (error-handling-policy.md §2), případně konkrétní pravidlo
/// (<c>rule</c>) a correlation id. Nedostupná síť má kód <see cref="NetworkUnavailable"/>.
/// </summary>
public sealed record ApiError(string Code, HttpStatusCode? Status, string? Detail, string? Rule = null, string? CorrelationId = null)
{
    public const string NetworkUnavailable = "network_unavailable";

    public bool IsNetworkFailure => Code == NetworkUnavailable;
}

/// <summary>Výsledek volání bez hodnoty.</summary>
public class ApiResult
{
    protected ApiResult(ApiError? error) => Error = error;

    public ApiError? Error { get; }

    public bool IsSuccess => Error is null;

    public static ApiResult Success() => new(null);

    public static ApiResult Failure(ApiError error) => new(error);
}

/// <summary>Výsledek volání s hodnotou.</summary>
public sealed class ApiResult<T> : ApiResult
{
    private readonly T? value;

    private ApiResult(T? value, ApiError? error)
        : base(error) => this.value = value;

    public T Value => IsSuccess ? value! : throw new InvalidOperationException($"Volání API selhalo: {Error!.Code}.");

    public static ApiResult<T> Success(T value) => new(value, null);

    public static new ApiResult<T> Failure(ApiError error) => new(default, error);
}
