namespace BudgetCivil.Application.Common.Exceptions;

public sealed class ConflictAppException(string message, string? code = null)
    : AppException(message, code)
{
    public override int StatusCode => 409;
}
