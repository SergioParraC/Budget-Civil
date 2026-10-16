namespace BudgetCivil.Application.Common.Exceptions;

public abstract class AppException(string message, string? code = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public abstract int StatusCode { get; }

    public string? Code { get; } = code;
}
