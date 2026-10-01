using BudgetCivil.Domain.Projects;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Projects.Repositories;

public interface IProjectRepository
{
    Task<UserAccount?> GetActorByIdAsync(Guid actorUserId, CancellationToken cancellationToken = default);

    Task<Project?> GetProjectByIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<Company?> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveDirectorInCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<Guid?> GetOldestActiveDirectorUserIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveSignatureAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddProjectAsync(Project project, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> GetProjectsAsync(
        Guid? companyId,
        ProjectStatus? status,
        string? search,
        int pageNumber,
        int pageSize,
        bool includeInhabilitado,
        CancellationToken cancellationToken = default);

    Task<int> CountProjectsAsync(
        Guid? companyId,
        ProjectStatus? status,
        string? search,
        bool includeInhabilitado,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
