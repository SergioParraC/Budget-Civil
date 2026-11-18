using BudgetCivil.Domain.Catalogs;
using BudgetCivil.Domain.Resources;
using BudgetCivil.Domain.Suppliers;
using BudgetCivil.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Tests.Integration;

/// <summary>
/// `T-I-36`/`T-I-37`: persistencia, FKs de ubicación, índices, `rowversion`, baja lógica, reutilización
/// entre proyectos y aislamiento (`REQ-PROVEEDORES-01`, `REQ-PROVEEDORES-11`, `REQ-PROVEEDORES-12`).
/// Verificación manual contra SQL Server local (`D-66`).
/// </summary>
public sealed class SupplierPersistenceTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Un_proveedor_guarda_ubicacion_rowversion_y_autoría()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);

        var supplier = Supplier.Create(
            data.Company.Id,
            "Cementos del Norte",
            "Ana Pérez",
            "3001234567",
            "contacto@cementos.example.com",
            "Calle 10 #20-30",
            "Entrega en obra",
            data.Country.Id,
            data.Department.Id,
            data.Municipality.Id,
            data.Director.Id,
            DateTime.UtcNow);

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        Assert.NotEmpty(supplier.RowVersion);

        var stored = await context.Suppliers
            .Include(existing => existing.Country)
            .Include(existing => existing.Department)
            .Include(existing => existing.Municipality)
            .SingleAsync(existing => existing.Id == supplier.Id);

        Assert.Equal(data.Country.Id, stored.CountryId);
        Assert.Equal(data.Department.Id, stored.DepartmentId);
        Assert.Equal(data.Municipality.Id, stored.MunicipalityId);
        Assert.Equal(data.Country.Name, stored.Country!.Name);
        Assert.Equal(data.Department.Name, stored.Department!.Name);
        Assert.Equal(data.Municipality.Name, stored.Municipality!.Name);
        Assert.True(stored.IsActive);
        Assert.Equal(data.Director.Id, stored.CreatedByUserId);
    }

    [Fact]
    public async Task El_mismo_proveedor_se_reutiliza_en_dos_proyectos_de_la_compania()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var sourceProject = TestDataFactory.CreateProject(data, "Origen");
        var targetProject = TestDataFactory.CreateProject(data, "Destino");
        context.Projects.AddRange(sourceProject, targetProject);
        await context.SaveChangesAsync();

        var supplier = Supplier.Create(
            data.Company.Id,
            "Cementos del Norte",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            data.Director.Id,
            DateTime.UtcNow);

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var resourceType = await context.ResourceTypes.SingleAsync(type => type.Code == ResourceTypeCatalog.Material);
        var first = Resource.Create(
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
            supplier.Id,
            data.Director.Id,
            DateTime.UtcNow);

        var second = Resource.Create(
            targetProject.Id,
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
            supplier.Id,
            data.Director.Id,
            DateTime.UtcNow);

        context.Resources.AddRange(first, second);
        await context.SaveChangesAsync();

        var supplierCount = await context.Suppliers.CountAsync(existing => existing.CompanyId == data.Company.Id);
        Assert.Equal(1, supplierCount);

        var linked = await context.Resources
            .Where(resource => resource.Id == first.Id || resource.Id == second.Id)
            .Select(resource => resource.SupplierId)
            .Distinct()
            .ToListAsync();

        Assert.Single(linked);
        Assert.Equal(supplier.Id, linked[0]);
    }

    [Fact]
    public async Task La_baja_logica_conserva_el_proveedor_y_su_insumo()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var project = TestDataFactory.CreateProject(data);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var supplier = Supplier.Create(
            data.Company.Id,
            "Cementos del Norte",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            data.Director.Id,
            DateTime.UtcNow);

        context.Suppliers.Add(supplier);
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
            supplier.Id,
            data.Director.Id,
            DateTime.UtcNow);

        context.Resources.Add(resource);
        await context.SaveChangesAsync();

        supplier.Deactivate(data.Director.Id, DateTime.UtcNow);
        await context.SaveChangesAsync();

        var storedSupplier = await context.Suppliers.SingleAsync(existing => existing.Id == supplier.Id);
        var storedResource = await context.Resources.SingleAsync(existing => existing.Id == resource.Id);

        Assert.False(storedSupplier.IsActive);
        Assert.Equal(supplier.Id, storedResource.SupplierId);
    }

    [Fact]
    public async Task Dos_proveedores_pueden_compartir_razon_social()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);

        for (var index = 0; index < 2; index++)
        {
            context.Suppliers.Add(Supplier.Create(
                data.Company.Id,
                "Cementos del Norte",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                data.Director.Id,
                DateTime.UtcNow));
        }

        await context.SaveChangesAsync();

        var duplicates = await context.Suppliers
            .CountAsync(existing => existing.CompanyId == data.Company.Id && existing.BusinessName == "Cementos del Norte");

        Assert.Equal(2, duplicates);
    }

    [Fact]
    public async Task El_aislamiento_por_compania_separa_los_directorios()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var firstCompany = await TestDataFactory.CreateCompanyWithDirectorAsync(context);
        var secondCompany = await TestDataFactory.CreateCompanyWithDirectorAsync(context);

        context.Suppliers.Add(Supplier.Create(
            firstCompany.Company.Id,
            "Cementos del Norte",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            firstCompany.Director.Id,
            DateTime.UtcNow));

        context.Suppliers.Add(Supplier.Create(
            secondCompany.Company.Id,
            "Cementos del Sur",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            secondCompany.Director.Id,
            DateTime.UtcNow));

        await context.SaveChangesAsync();

        var firstDirectory = await context.Suppliers
            .Where(supplier => supplier.CompanyId == firstCompany.Company.Id)
            .ToListAsync();

        Assert.Single(firstDirectory);
        Assert.Equal("Cementos del Norte", firstDirectory[0].BusinessName);
    }

    [Fact]
    public async Task La_fk_de_ubicacion_rechaza_un_pais_inexistente()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);

        var supplier = Supplier.Create(
            data.Company.Id,
            "Cementos del Norte",
            null,
            null,
            null,
            null,
            null,
            Guid.NewGuid(),
            null,
            null,
            data.Director.Id,
            DateTime.UtcNow);

        context.Suppliers.Add(supplier);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task La_edicion_concurrente_actualiza_rowversion()
    {
        await using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var data = await TestDataFactory.CreateCompanyWithDirectorAsync(context);

        var supplier = Supplier.Create(
            data.Company.Id,
            "Cementos del Norte",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            data.Director.Id,
            DateTime.UtcNow);

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var originalVersion = supplier.RowVersion.ToArray();

        supplier.Update(
            "Cementos del Sur",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            data.Director.Id,
            DateTime.UtcNow);

        await context.SaveChangesAsync();

        Assert.False(originalVersion.SequenceEqual(supplier.RowVersion));

        var catalogCheck = context.Model.FindEntityType(typeof(Supplier));
        Assert.NotNull(catalogCheck);
        Assert.Empty(catalogCheck!.GetIndexes().Where(index =>
            index.IsUnique &&
            index.Properties.Count == 1 &&
            index.Properties[0].Name is nameof(Supplier.BusinessName) or nameof(Supplier.Email)));
    }

    [Fact]
    public void Las_fks_y_los_indices_del_modelo_son_los_esperados()
    {
        using var context = SqlServerTestDatabase.CreateContext(fixture.ConnectionString);
        var supplierType = context.Model.FindEntityType(typeof(Supplier));
        Assert.NotNull(supplierType);
        Assert.Equal("suppliers", supplierType!.GetTableName());

        var foreignKeys = supplierType.GetForeignKeys().ToList();
        Assert.Contains(foreignKeys, key => key.PrincipalEntityType.ClrType == typeof(BudgetCivil.Domain.Users.Entities.Company));
        Assert.Contains(foreignKeys, key => key.PrincipalEntityType.ClrType == typeof(Country));
        Assert.Contains(foreignKeys, key => key.PrincipalEntityType.ClrType == typeof(Department));
        Assert.Contains(foreignKeys, key => key.PrincipalEntityType.ClrType == typeof(Municipality));
        Assert.All(foreignKeys, key => Assert.Equal(DeleteBehavior.Restrict, key.DeleteBehavior));

        var indexNames = supplierType.GetIndexes().Select(index => index.GetDatabaseName()).ToList();
        Assert.Contains("IX_dbo_suppliers_company_id_business_name", indexNames);
        Assert.Contains("IX_dbo_suppliers_company_id_country_id", indexNames);
        Assert.Contains("IX_dbo_suppliers_company_id_department_id", indexNames);
        Assert.Contains("IX_dbo_suppliers_company_id_municipality_id", indexNames);
        Assert.Contains("IX_dbo_suppliers_company_id_is_active", indexNames);
        Assert.DoesNotContain(indexNames, name => name?.Contains("business_name", StringComparison.Ordinal) == true && name.StartsWith("UQ_", StringComparison.Ordinal));

        var resourceType = context.Model.FindEntityType(typeof(Resource));
        Assert.NotNull(resourceType);
        var supplierProperty = resourceType!.FindProperty(nameof(Resource.SupplierId));
        Assert.NotNull(supplierProperty);
        Assert.True(supplierProperty!.IsNullable);
        Assert.Contains(
            resourceType.GetIndexes().Select(index => index.GetDatabaseName()),
            name => name == "IX_dbo_resources_supplier_id");
    }
}
