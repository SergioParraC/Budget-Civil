namespace BudgetCivil.Application.Common.Exceptions;

public sealed class ForbiddenAppException(string message, string? code = null)
    : AppException(message, code)
{
    public override int StatusCode => 403;
}
