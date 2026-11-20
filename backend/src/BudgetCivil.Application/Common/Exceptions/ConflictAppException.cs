namespace BudgetCivil.Application.Common.Exceptions;

public sealed class ConflictAppException(
    string message,
    string? code = null,
    IReadOnlyDictionary<string, object?>? extensions = null)
    : AppException(message, code, extensions: extensions)
{
    public override int StatusCode => 409;
}
