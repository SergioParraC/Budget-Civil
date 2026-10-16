using BudgetCivil.Domain.Projects;

namespace BudgetCivil.Application.Projects.Repositories;

public interface IProjectTransitionRepository
{
    Task AddStatusTransitionAsync(ProjectStatusTransition transition, CancellationToken cancellationToken = default);

    Task AddVisibilityTransitionAsync(ProjectVisibilityTransition transition, CancellationToken cancellationToken = default);
}
