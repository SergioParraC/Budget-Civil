namespace BudgetCivil.Application.Common.Exceptions;

public sealed class LockedAppException(string message, string? code = null)
    : AppException(message, code)
{
    public override int StatusCode => 423;
}
