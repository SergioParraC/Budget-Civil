using BudgetCivil.Domain.Resources;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration;

/// <summary>T-I-33/T-I-34: persistencia, `rowversion`, baja lógica y copia independiente (`D-49`, `D-50`).</summary>
public sealed class ResourcePersistenceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Un_insumo_guarda_rowversion_y_la_baja_logica_lo_oculta()
    {
        Guid resourceId;

        await using (var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
            var project = TestDataFactory.CreateProject(data);
            context.Projects.Add(project);
            await context.SaveChangesAsync();

            var resourceType = await context.ResourceTypes.SingleAsync(type => type.Code == ResourceTypeCatalog.Material);
            var resource = Resource.Create(
                project.Id,
                resourceType.Id,
                data.Municipality.Id,
                "Cemento gris",
                "m2",
                10m,
                "Marca",
                null,
                null,
                null,
                null,
                data.Director.Id,
                DateTime.UtcNow);

            context.Resources.Add(resource);
            await context.SaveChangesAsync();

            Assert.NotEmpty(resource.RowVersion);
            resourceId = resource.Id;
        }

        await using var verificationContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var stored = await verificationContext.Resources.SingleAsync(resource => resource.Id == resourceId);
        stored.IsActive = false;
        stored.DeletedAtUtc = DateTime.UtcNow;
        await verificationContext.SaveChangesAsync();

        var visible = await verificationContext.Resources.AnyAsync(resource => resource.Id == resourceId);
        Assert.False(visible);
    }

    [Fact]
    public async Task La_copia_es_independiente_del_insumo_de_origen()
    {
        Guid sourceId;

        await using (var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
            var sourceProject = TestDataFactory.CreateProject(data, "Origen");
            var targetProject = TestDataFactory.CreateProject(data, "Destino");
            context.Projects.AddRange(sourceProject, targetProject);
            await context.SaveChangesAsync();

            var resourceType = await context.ResourceTypes.SingleAsync(type => type.Code == ResourceTypeCatalog.Material);
            var source = Resource.Create(
                sourceProject.Id,
                resourceType.Id,
                data.Municipality.Id,
                "Cemento gris",
                "m2",
                10m,
                "Marca",
                null,
                null,
                null,
                null,
                data.Director.Id,
                DateTime.UtcNow);

            context.Resources.Add(source);
            await context.SaveChangesAsync();

            var copy = source.CopyTo(targetProject.Id, data.Director.Id, DateTime.UtcNow);
            context.Resources.Add(copy);
            await context.SaveChangesAsync();

            Assert.Equal(source.Id, copy.OriginResourceId);
            Assert.Equal(10m, copy.UnitPrice);

            copy.UnitPrice = 99m;
            await context.SaveChangesAsync();
            sourceId = source.Id;
        }

        await using var verificationContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var storedSource = await verificationContext.Resources.SingleAsync(resource => resource.Id == sourceId);
        Assert.Equal(10m, storedSource.UnitPrice);
    }
}
