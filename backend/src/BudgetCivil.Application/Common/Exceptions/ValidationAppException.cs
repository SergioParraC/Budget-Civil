namespace BudgetCivil.Application.Common.Exceptions;

public sealed class ValidationAppException(string message, string? code = null)
    : AppException(message, code)
{
    public override int StatusCode => 400;
}
