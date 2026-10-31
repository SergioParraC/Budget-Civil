using BudgetCivil.Application.Users.Bootstrap.Dtos;

namespace BudgetCivil.Application.Users.Bootstrap.Repositories;

public interface IBootstrapRepository
{
    Task<bool> AnyAdministratorExistsAsync(CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    Task<(Guid AdministratorId, Guid AuditEventId)> CreateFirstAdministratorAsync(
        BootstrapRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BootstrapAuditRecordResponse>> GetBootstrapAuditAsync(CancellationToken cancellationToken = default);
}
