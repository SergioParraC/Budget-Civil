namespace BudgetCivil.Application.Common.Exceptions;

public abstract class AppException(
    string message,
    string? code = null,
    Exception? innerException = null,
    IReadOnlyDictionary<string, object?>? extensions = null)
    : Exception(message, innerException)
{
    public abstract int StatusCode { get; }

    public string? Code { get; } = code;

    /// <summary>Extensiones adicionales del `ProblemDetails` (por ejemplo `pendingResources`).</summary>
    public IReadOnlyDictionary<string, object?>? Extensions { get; } = extensions;
}
