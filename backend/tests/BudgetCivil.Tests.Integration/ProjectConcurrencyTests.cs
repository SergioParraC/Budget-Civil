using BudgetCivil.Domain.Projects;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration;

/// <summary>T-I-24: `rowversion` y rechazo de la actualización desactualizada (`D-97`).</summary>
public sealed class ProjectConcurrencyTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Una_actualizacion_con_la_version_anterior_es_rechazada()
    {
        Guid projectId;

        await using (var setupContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var data = await TestDataFactory.CreateCompanyWithDirectorAsync(setupContext);
            var project = TestDataFactory.CreateProject(data);

            setupContext.Projects.Add(project);
            await setupContext.SaveChangesAsync();

            projectId = project.Id;
        }

        await using var firstContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        await using var staleContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var firstCopy = await firstContext.Projects.SingleAsync(project => project.Id == projectId);
        var staleCopy = await staleContext.Projects.SingleAsync(project => project.Id == projectId);

        firstCopy.Name = "Nombre vigente";
        firstCopy.UpdatedAtUtc = DateTime.UtcNow;
        await firstContext.SaveChangesAsync();

        staleCopy.Name = "Nombre desactualizado";
        staleCopy.UpdatedAtUtc = DateTime.UtcNow;

        // SC-PROYECTOS-24: la versión anterior ya no es la vigente.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleContext.SaveChangesAsync());

        await using var verificationContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var storedProject = await verificationContext.Projects.SingleAsync(project => project.Id == projectId);

        Assert.Equal("Nombre vigente", storedProject.Name);
        Assert.Equal(ProjectStatus.PorCotizar, storedProject.Status);
    }

    [Fact]
    public async Task La_actualizacion_con_la_version_vigente_avanza_la_version()
    {
        Guid projectId;

        await using (var setupContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var data = await TestDataFactory.CreateCompanyWithDirectorAsync(setupContext);
            var project = TestDataFactory.CreateProject(data);

            setupContext.Projects.Add(project);
            await setupContext.SaveChangesAsync();

            projectId = project.Id;
        }

        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var projectCopy = await context.Projects.SingleAsync(project => project.Id == projectId);
        var previousVersion = projectCopy.RowVersion.ToArray();

        projectCopy.Name = "Nombre actualizado";
        projectCopy.UpdatedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();

        Assert.NotEqual(previousVersion, projectCopy.RowVersion);
    }
}
