using BudgetCivil.Application.Users.Bootstrap.Dtos;
using BudgetCivil.Application.Users.Bootstrap.Repositories;

namespace BudgetCivil.Application.Users.Bootstrap.UseCases;

public sealed class GetBootstrapAuditUseCase(IBootstrapRepository bootstrapRepository)
{
    public Task<IReadOnlyList<BootstrapAuditRecordResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return bootstrapRepository.GetBootstrapAuditAsync(cancellationToken);
    }
}
