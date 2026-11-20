using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Domain.Apus;
using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Domain.Resources;
using BudgetCivil.Infrastructure.Persistence;
using BudgetCivil.Infrastructure.Persistence.Repositories;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration;

/// <summary>
/// `T-I-47`/`T-I-48`: migración del catálogo de unidades y FKs que sostienen la baja física del insumo
/// (`REQ-UNIDADES-01`, `REQ-INSUMOS-18`…`REQ-INSUMOS-20`).
/// </summary>
public sealed class MeasurementUnitPersistenceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task La_migracion_crea_measurement_units_y_amplia_apus_unit()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains(appliedMigrations, migration => migration.EndsWith("_units_add_catalog", StringComparison.Ordinal));

        var columns = await ReadTableColumnsAsync(context, "measurement_units");
        Assert.Contains("id", columns);
        Assert.Contains("short_unit", columns);
        Assert.Contains("unit", columns);

        var apusUnit = await ReadColumnAsync(context, "apus", "unit");
        Assert.Equal("varchar", apusUnit.DataType);
        Assert.Equal(10, apusUnit.MaxLength);

        var uniqueIndexCount = await ReadIntAsync(
            context,
            "SELECT COUNT(*) FROM sys.indexes i " +
            "JOIN sys.tables t ON t.object_id = i.object_id " +
            "WHERE t.name = 'measurement_units' AND i.name = 'UQ_dbo_measurement_units_short_unit' AND i.is_unique = 1");
        Assert.Equal(1, uniqueIndexCount);
    }

    [Fact]
    public async Task El_indice_unico_impide_repetir_la_sigla_sin_distinguir_mayusculas()
    {
        var shortUnit = $"T{Guid.NewGuid().ToString("N")[..9].ToUpperInvariant()}";

        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var repository = new MeasurementUnitRepository(context);

        await repository.AddAsync(MeasurementUnit.Create(shortUnit, "Unidad de prueba"));
        await repository.SaveChangesAsync();

        var stored = await repository.ListAsync();
        Assert.Contains(stored, unit => unit.ShortUnit == shortUnit);

        await repository.AddAsync(MeasurementUnit.Create(shortUnit.ToLowerInvariant(), "Otra unidad"));

        var conflict = await Assert.ThrowsAsync<ConflictAppException>(() => repository.SaveChangesAsync());
        Assert.Equal("UNIT_ALREADY_EXISTS", conflict.Code);
        context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task La_baja_fisica_elimina_el_insumo_y_las_referencias_la_bloquean()
    {
        Guid freeResourceId;
        Guid usedResourceId;
        Guid originResourceId;
        Guid componentResourceId;
        Guid matrixResourceId;

        await using (var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString))
        {
            var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
            var project = TestDataFactory.CreateProject(data);
            context.Projects.Add(project);
            await context.SaveChangesAsync();

            var materialType = await context.ResourceTypes.SingleAsync(type => type.Code == ResourceTypeCatalog.Material);
            var matrixType = await context.ResourceTypes.SingleAsync(type => type.Code == ResourceTypeCatalog.Matrix);
            var now = DateTime.UtcNow;

            var free = CreateResource(project.Id, materialType.Id, data.Municipality.Id, "Libre", data.Director.Id, now);
            var used = CreateResource(project.Id, materialType.Id, data.Municipality.Id, "Usado", data.Director.Id, now);
            var origin = CreateResource(project.Id, materialType.Id, data.Municipality.Id, "Origen", data.Director.Id, now);
            var matrixResource = CreateResource(project.Id, matrixType.Id, data.Municipality.Id, "Matriz", data.Director.Id, now);
            var component = CreateResource(project.Id, materialType.Id, data.Municipality.Id, "Componente", data.Director.Id, now);

            context.Resources.AddRange(free, used, origin, matrixResource, component);
            await context.SaveChangesAsync();

            var copy = origin.CopyTo(project.Id, data.Director.Id, now);
            context.Resources.Add(copy);

            var apu = Apu.Create(project.Id, "APU de prueba", "M2", data.Director.Id, now);
            apu.AddLine(used.Id, 1m, 0m, data.Director.Id, now);
            context.Apus.Add(apu);

            context.MatrixComponents.Add(MatrixComponent.Create(matrixResource.Id, component.Id, 1m));
            await context.SaveChangesAsync();

            freeResourceId = free.Id;
            usedResourceId = used.Id;
            originResourceId = origin.Id;
            componentResourceId = component.Id;
            matrixResourceId = matrixResource.Id;
        }

        await using var verificationContext = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var repository = new ResourceRepository(verificationContext);

        Assert.False(await repository.HasReferencesAsync(freeResourceId));
        var freeResource = await verificationContext.Resources
            .SingleAsync(resource => resource.Id == freeResourceId);
        await repository.DeleteAsync(freeResource);
        verificationContext.ChangeTracker.Clear();
        Assert.False(await verificationContext.Resources.IgnoreQueryFilters()
            .AnyAsync(resource => resource.Id == freeResourceId));

        Assert.True(await repository.HasReferencesAsync(usedResourceId));
        var usedResource = await verificationContext.Resources
            .SingleAsync(resource => resource.Id == usedResourceId);
        var conflict = await Assert.ThrowsAsync<ConflictAppException>(
            () => repository.DeleteAsync(usedResource));
        Assert.Equal("RESOURCE_IN_USE", conflict.Code);
        verificationContext.ChangeTracker.Clear();

        Assert.True(await repository.HasReferencesAsync(originResourceId));
        Assert.True(await repository.HasReferencesAsync(componentResourceId));

        var matrix = await verificationContext.Resources
            .SingleAsync(resource => resource.Id == matrixResourceId);
        await repository.DeleteAsync(matrix);
        verificationContext.ChangeTracker.Clear();
        Assert.False(await verificationContext.MatrixComponents.AnyAsync(
            component => component.MatrixResourceId == matrixResourceId));
    }

    [Fact]
    public async Task El_lector_de_tipos_excluye_los_inactivos()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var materialTypeId = await context.ResourceTypes
            .Where(type => type.Code == ResourceTypeCatalog.Material)
            .Select(type => type.Id)
            .SingleAsync();

        var inactive = new ResourceType
        {
            Id = Guid.NewGuid(),
            Code = $"INACTIVO_{suffix}",
            Name = $"Inactivo {suffix}",
            IsActive = false
        };

        context.ResourceTypes.Add(inactive);
        await context.SaveChangesAsync();

        var reader = new ResourceTypeReader(context);
        var types = await reader.ListActiveAsync();

        Assert.Contains(types, type => type.Id == materialTypeId);
        Assert.DoesNotContain(types, type => type.Id == inactive.Id);

        context.ResourceTypes.Remove(inactive);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task El_repositorio_filtra_insumos_por_unidad_sin_distinguir_mayusculas()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var project = TestDataFactory.CreateProject(data, "Proyecto por unidad");
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var materialType = await context.ResourceTypes.SingleAsync(type => type.Code == ResourceTypeCatalog.Material);
        var now = DateTime.UtcNow;

        context.Resources.AddRange(
            CreateResource(project.Id, materialType.Id, data.Municipality.Id, "Cemento", data.Director.Id, now, "m2"),
            CreateResource(project.Id, materialType.Id, data.Municipality.Id, "Madera", data.Director.Id, now, "ML"));
        await context.SaveChangesAsync();

        var repository = new ResourceRepository(context);
        var resources = await repository.ListByUnitAsync(project.Id, "M2");

        Assert.Single(resources);
        Assert.Equal("Cemento", resources[0].Description);
        Assert.Equal("m2", resources[0].Unit);
    }

    private static Resource CreateResource(
        Guid projectId,
        Guid resourceTypeId,
        Guid municipalityId,
        string description,
        Guid actorUserId,
        DateTime utcNow,
        string unit = "M2")
    {
        return Resource.Create(
            projectId,
            resourceTypeId,
            municipalityId,
            description,
            unit,
            10m,
            "Marca",
            distributor: null,
            userKey: null,
            details: null,
            contactNumber: null,
            supplierId: null,
            actorUserId,
            utcNow);
    }

    private static async Task<List<string>> ReadTableColumnsAsync(
        BudgetCivilDbContext context,
        string tableName)
    {
        return await ReadStringsAsync(
            context,
            $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'");
    }

    private static async Task<(string DataType, int? MaxLength)> ReadColumnAsync(
        BudgetCivilDbContext context,
        string tableName,
        string columnName)
    {
        var connection = context.Database.GetDbConnection();
        var mustClose = connection.State != System.Data.ConnectionState.Open;

        if (mustClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                $"SELECT DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS " +
                $"WHERE TABLE_NAME = '{tableName}' AND COLUMN_NAME = '{columnName}'";
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());

            return (
                reader.GetString(0),
                reader.IsDBNull(1) ? null : Convert.ToInt32(reader.GetValue(1)));
        }
        finally
        {
            if (mustClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<int> ReadIntAsync(BudgetCivilDbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        var mustClose = connection.State != System.Data.ConnectionState.Open;

        if (mustClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        finally
        {
            if (mustClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<List<string>> ReadStringsAsync(BudgetCivilDbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        var mustClose = connection.State != System.Data.ConnectionState.Open;

        if (mustClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var values = new List<string>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetString(0));
            }

            return values;
        }
        finally
        {
            if (mustClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}