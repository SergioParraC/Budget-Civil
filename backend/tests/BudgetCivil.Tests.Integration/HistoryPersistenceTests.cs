using BudgetCivil.Domain.Projects;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration;

/// <summary>T-I-25: historial de estado y de visibilidad como tablas de solo inserción (`D-92`).</summary>
public sealed class HistoryPersistenceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Cada_cambio_de_estado_y_de_visibilidad_deja_su_registro()
    {
        Guid projectId;
        Guid actorUserId;

        await using (var setupContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var data = await TestDataFactory.CreateCompanyWithDirectorAsync(setupContext);
            var project = TestDataFactory.CreateProject(data);

            setupContext.Projects.Add(project);
            await setupContext.SaveChangesAsync();

            projectId = project.Id;
            actorUserId = data.Director.Id;
        }

        var changedAtUtc = DateTime.UtcNow;

        await using (var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            context.ProjectStatusTransitions.Add(new ProjectStatusTransition
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                PreviousStatus = ProjectStatus.PorCotizar,
                NewStatus = ProjectStatus.FaltaInformacion,
                ChangedByUserId = actorUserId,
                ChangedAtUtc = changedAtUtc,
                Reason = "Prueba de historial",
                TransitionKind = ProjectTransitionKind.Ordinary
            });

            context.ProjectVisibilityTransitions.Add(new ProjectVisibilityTransition
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                PreviousIsPublic = false,
                NewIsPublic = true,
                ChangedByUserId = actorUserId,
                ChangedAtUtc = changedAtUtc,
                Reason = "Prueba de visibilidad"
            });

            await context.SaveChangesAsync();
        }

        await using var verificationContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var statusHistory = await verificationContext.ProjectStatusTransitions
            .SingleAsync(transition => transition.ProjectId == projectId);

        Assert.Equal(ProjectStatus.PorCotizar, statusHistory.PreviousStatus);
        Assert.Equal(ProjectStatus.FaltaInformacion, statusHistory.NewStatus);
        Assert.Equal(actorUserId, statusHistory.ChangedByUserId);
        Assert.Equal("Prueba de historial", statusHistory.Reason);

        var visibilityHistory = await verificationContext.ProjectVisibilityTransitions
            .SingleAsync(transition => transition.ProjectId == projectId);

        Assert.False(visibilityHistory.PreviousIsPublic);
        Assert.True(visibilityHistory.NewIsPublic);
        Assert.Equal(actorUserId, visibilityHistory.ChangedByUserId);
    }
}
