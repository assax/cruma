namespace Cruma.Server.Infrastructure;

/// <summary>
/// Očekávané selhání aplikační služby (error-handling-policy.md P-2): stabilní kód, popis bez obsahu poznámek
/// a volitelná rozšíření ProblemDetails.
/// </summary>
public sealed record ServiceError(string Code, string Detail, IReadOnlyDictionary<string, object?>? Extensions = null)
{
    public static ServiceError NotFound() => new(ErrorCodes.NotFound, "Položka neexistuje.");

    public static ServiceError Validation(string detail, string? rule = null) =>
        new(ErrorCodes.ValidationFailed, detail, rule is null ? null : new Dictionary<string, object?> { ["rule"] = rule });
}

/// <summary>Výsledek operace bez hodnoty.</summary>
public class ServiceResult
{
    protected ServiceResult(ServiceError? error) => Error = error;

    public ServiceError? Error { get; }

    public bool IsSuccess => Error is null;

    public static ServiceResult Success() => new(null);

    public static ServiceResult Failure(ServiceError error) => new(error);

    public static implicit operator ServiceResult(ServiceError error) => new(error);
}

/// <summary>Výsledek operace s hodnotou.</summary>
public sealed class ServiceResult<T> : ServiceResult
{
    private readonly T? value;

    private ServiceResult(T? value, ServiceError? error)
        : base(error) => this.value = value;

    public T Value => IsSuccess ? value! : throw new InvalidOperationException("Neúspěšný výsledek nemá hodnotu.");

    public static ServiceResult<T> Success(T value) => new(value, null);

    public static new ServiceResult<T> Failure(ServiceError error) => new(default, error);

    public static implicit operator ServiceResult<T>(T value) => Success(value);

    public static implicit operator ServiceResult<T>(ServiceError error) => Failure(error);
}
