using BudgetCivil.Domain.Projects;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration;

/// <summary>T-I-30: una sola configuración por proyecto (`D-93`).</summary>
public sealed class ProjectConfigurationPersistenceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task El_proyecto_se_crea_con_su_unica_configuracion()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var project = TestDataFactory.CreateProject(data);

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        await using var verificationContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var storedProject = await verificationContext.Projects
            .Include(existingProject => existingProject.Configuration)
            .SingleAsync(existingProject => existingProject.Id == project.Id);

        Assert.Equal(data.BaseCurrency.Id, storedProject.Configuration.BaseCurrencyId);
        Assert.Equal(1000m, storedProject.Configuration.BudgetLimit);
        Assert.Equal(Project.DefaultSignatureMode, storedProject.Configuration.SignatureMode);
        Assert.False(storedProject.Configuration.RequiresAuditor);
        Assert.NotEmpty(storedProject.Configuration.RowVersion);
    }

    [Fact]
    public async Task Una_segunda_configuracion_del_mismo_proyecto_es_rechazada()
    {
        Guid projectId;
        Guid baseCurrencyId;

        await using (var setupContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var data = await TestDataFactory.CreateCompanyWithDirectorAsync(setupContext);
            var project = TestDataFactory.CreateProject(data);

            setupContext.Projects.Add(project);
            await setupContext.SaveChangesAsync();

            projectId = project.Id;
            baseCurrencyId = data.BaseCurrency.Id;
        }

        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        context.ProjectConfigurations.Add(new ProjectConfiguration
        {
            ProjectId = projectId,
            BaseCurrencyId = baseCurrencyId
        });

        // SC-PROYECTOS-23: la clave primaria (project_id) impide la segunda configuración.
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
