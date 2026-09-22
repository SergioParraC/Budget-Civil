namespace BudgetCivil.Application.Common.Auditing;

public interface ISecurityAuditRepository
{
    Task AppendAsync(SecurityAuditWriteRequest request, CancellationToken cancellationToken = default);
}
