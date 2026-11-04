namespace BudgetCivil.Application.Common.Exceptions;

public sealed class NotFoundAppException(string message, string? code = null)
    : AppException(message, code)
{
    public override int StatusCode => 404;
}
