using BudgetCivil.Application.Projects.Repositories;
using BudgetCivil.Domain.Projects;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class ProjectTransitionRepository(BudgetCivilDbContext dbContext) : IProjectTransitionRepository
{
    public Task AddStatusTransitionAsync(ProjectStatusTransition transition, CancellationToken cancellationToken = default)
    {
        dbContext.ProjectStatusTransitions.Add(transition);
        return Task.CompletedTask;
    }

    public Task AddVisibilityTransitionAsync(ProjectVisibilityTransition transition, CancellationToken cancellationToken = default)
    {
        dbContext.ProjectVisibilityTransitions.Add(transition);
        return Task.CompletedTask;
    }
}
