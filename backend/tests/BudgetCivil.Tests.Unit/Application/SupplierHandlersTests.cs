using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Suppliers.Commands.ChangeSupplierStatus;
using BudgetCivil.Application.Suppliers.Commands.CreateSupplier;
using BudgetCivil.Application.Suppliers.Commands.UpdateSupplier;
using BudgetCivil.Application.Suppliers.Dtos;
using BudgetCivil.Application.Suppliers.Queries;
using BudgetCivil.Application.Suppliers.Repositories;
using BudgetCivil.Application.Suppliers.Services;
using BudgetCivil.Domain.Suppliers;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-U-15`, `T-U-16` y `T-U-17`: validadores, casos de uso, selector y alerta
/// (`SC-PROVEEDORES-01`…`SC-PROVEEDORES-46`).
/// </summary>
public sealed class SupplierHandlersTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid OtherCompanyId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ColombiaId = Guid.NewGuid();
    private static readonly Guid AntioquiaId = Guid.NewGuid();
    private static readonly Guid MedellinId = Guid.NewGuid();
    private static readonly Guid BogotaId = Guid.NewGuid();

    [Fact]
    public async Task Create_guarda_el_proveedor_en_la_compania_del_actor()
    {
        // SC-PROVEEDORES-01 y SC-PROVEEDORES-04: la compañía sale del actor, no del cuerpo.
        var repository = new Mock<ISupplierRepository>();
        Supplier? created = null;
        SetupAddAndReload(repository, supplier => created = supplier);

        var handler = new CreateSupplierCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            new CreateSupplierCommandValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            new CreateSupplierCommand { BusinessName = "Cementos del Norte" });

        response.CompanyId.Should().Be(CompanyId);
        response.BusinessName.Should().Be("Cementos del Norte");
        response.ContactName.Should().BeNull();
        response.Email.Should().BeNull();
        response.IsActive.Should().BeTrue();
        created.Should().NotBeNull();
        created!.CompanyId.Should().Be(CompanyId);
        created.CreatedByUserId.Should().Be(ActorId);
    }

    [Fact]
    public async Task Create_con_todos_los_datos_los_devuelve()
    {
        // SC-PROVEEDORES-05.
        var repository = new Mock<ISupplierRepository>();
        SetupAddAndReload(repository, _ => { });
        SetupLocation(repository, departmentCountryId: ColombiaId, municipalityDepartmentId: AntioquiaId);

        var handler = new CreateSupplierCommandHandler(
            AccessService(CompanyId, "PRESUPUESTADOR"),
            repository.Object,
            new CreateSupplierCommandValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            new CreateSupplierCommand
            {
                BusinessName = "Cementos del Norte",
                ContactName = "Ana Pérez",
                ContactPhone = "3001234567",
                Email = "contacto@cementos.example.com",
                Address = "Calle 10 #20-30",
                Observations = "Entrega en obra",
                CountryId = ColombiaId,
                DepartmentId = AntioquiaId,
                MunicipalityId = MedellinId
            });

        response.ContactName.Should().Be("Ana Pérez");
        response.Email.Should().Be("contacto@cementos.example.com");
        response.Observations.Should().Be("Entrega en obra");
        response.MunicipalityId.Should().Be(MedellinId);
    }

    [Fact]
    public async Task Create_rechaza_la_razon_social_vacia()
    {
        // SC-PROVEEDORES-06.
        var handler = new CreateSupplierCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            new Mock<ISupplierRepository>().Object,
            new CreateSupplierCommandValidator());

        var action = () => handler.ExecuteAsync(ActorId, new CreateSupplierCommand { BusinessName = " " });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Create_rechaza_el_correo_invalido()
    {
        // SC-PROVEEDORES-07.
        var handler = new CreateSupplierCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            new Mock<ISupplierRepository>().Object,
            new CreateSupplierCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            new CreateSupplierCommand
            {
                BusinessName = "Cementos del Norte",
                Email = "correo-invalido"
            });

        await action.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task Create_admite_los_campos_opcionales_ausentes()
    {
        // SC-PROVEEDORES-08.
        var repository = new Mock<ISupplierRepository>();
        SetupAddAndReload(repository, _ => { });

        var handler = new CreateSupplierCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            new CreateSupplierCommandValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            new CreateSupplierCommand
            {
                BusinessName = "Cementos del Norte",
                Email = "contacto@cementos.example.com"
            });

        response.ContactName.Should().BeNull();
        response.Address.Should().BeNull();
        response.Observations.Should().BeNull();
    }

    [Fact]
    public async Task Create_rechaza_un_municipio_de_otro_departamento()
    {
        // SC-PROVEEDORES-10.
        var repository = new Mock<ISupplierRepository>();
        SetupLocation(repository, departmentCountryId: ColombiaId, municipalityDepartmentId: BogotaId);

        var handler = new CreateSupplierCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            new CreateSupplierCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            new CreateSupplierCommand
            {
                BusinessName = "Cementos del Norte",
                CountryId = ColombiaId,
                DepartmentId = AntioquiaId,
                MunicipalityId = MedellinId
            });

        await action.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task Create_rechaza_un_municipio_inexistente()
    {
        // SC-PROVEEDORES-12.
        var repository = new Mock<ISupplierRepository>();
        SetupLocation(repository, municipalityExists: false);

        var handler = new CreateSupplierCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            new CreateSupplierCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            new CreateSupplierCommand
            {
                BusinessName = "Cementos del Norte",
                MunicipalityId = MedellinId
            });

        await action.Should().ThrowAsync<ValidationAppException>();
    }

    [Fact]
    public async Task Create_admite_la_ubicacion_parcial()
    {
        // SC-PROVEEDORES-09 y SC-PROVEEDORES-11.
        var repository = new Mock<ISupplierRepository>();
        SetupAddAndReload(repository, _ => { });
        SetupLocation(repository);

        var handler = new CreateSupplierCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            new CreateSupplierCommandValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            new CreateSupplierCommand { BusinessName = "Cementos del Norte", CountryId = ColombiaId });

        response.CountryId.Should().Be(ColombiaId);
        response.DepartmentId.Should().BeNull();
        response.MunicipalityId.Should().BeNull();
    }

    [Fact]
    public async Task Create_no_impone_unicidad_de_razon_social()
    {
        // SC-PROVEEDORES-20 y SC-PROVEEDORES-21.
        var repository = new Mock<ISupplierRepository>();
        SetupAddAndReload(repository, _ => { });

        var handler = new CreateSupplierCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            new CreateSupplierCommandValidator());

        var first = await handler.ExecuteAsync(
            ActorId,
            new CreateSupplierCommand { BusinessName = "Cementos del Norte" });
        var second = await handler.ExecuteAsync(
            ActorId,
            new CreateSupplierCommand { BusinessName = "Cementos del Norte" });

        first.BusinessName.Should().Be(second.BusinessName);
        repository.Verify(
            repo => repo.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Update_aplica_cambios_y_autoria()
    {
        // SC-PROVEEDORES-45.
        var supplier = CreateSupplier();
        supplier.RowVersion = [1, 2, 3];
        var repository = new Mock<ISupplierRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);
        repository
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SetupLocation(repository);

        var handler = new UpdateSupplierCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            new UpdateSupplierCommandValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            SupplierId,
            new UpdateSupplierCommand
            {
                BusinessName = "Cementos del Sur",
                Email = "ventas@cementos.example.com",
                RowVersion = Convert.ToBase64String(supplier.RowVersion)
            });

        response.BusinessName.Should().Be("Cementos del Sur");
        response.LastModifiedBy.Should().Be(ActorId);
        supplier.CreatedByUserId.Should().Be(ActorId);
    }

    [Fact]
    public async Task Update_con_version_desactualizada_devuelve_conflicto()
    {
        // SC-PROVEEDORES-44.
        var supplier = CreateSupplier();
        supplier.RowVersion = [1, 2, 3];
        var repository = new Mock<ISupplierRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        var handler = new UpdateSupplierCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            new UpdateSupplierCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            SupplierId,
            new UpdateSupplierCommand
            {
                BusinessName = "Cementos del Sur",
                RowVersion = Convert.ToBase64String([9, 9, 9])
            });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be("SUPPLIER_CONCURRENCY_CONFLICT");
    }

    [Fact]
    public async Task ChangeStatus_da_de_baja_y_reactiva()
    {
        // SC-PROVEEDORES-40 y SC-PROVEEDORES-43.
        var supplier = CreateSupplier();
        var repository = new Mock<ISupplierRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);
        repository
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ChangeSupplierStatusCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            new ChangeSupplierStatusCommandValidator());

        var deactivated = await handler.ExecuteAsync(
            ActorId,
            SupplierId,
            new ChangeSupplierStatusCommand { IsActive = false });

        deactivated.IsActive.Should().BeFalse();
        supplier.Id.Should().Be(SupplierId);

        var reactivated = await handler.ExecuteAsync(
            ActorId,
            SupplierId,
            new ChangeSupplierStatusCommand { IsActive = true });

        reactivated.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeStatus_no_alcanza_a_un_proveedor_de_otra_compania()
    {
        // SC-PROVEEDORES-03: no se revela la existencia de un proveedor de otra compañía.
        var repository = new Mock<ISupplierRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Supplier
            {
                Id = SupplierId,
                CompanyId = OtherCompanyId,
                BusinessName = "Otra compañía",
                IsActive = true
            });

        var handler = new ChangeSupplierStatusCommandHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            new ChangeSupplierStatusCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            SupplierId,
            new ChangeSupplierStatusCommand { IsActive = false });

        var exception = await action.Should().ThrowAsync<NotFoundAppException>();
        exception.Which.Code.Should().Be("SUPPLIER_NOT_FOUND");
    }

    [Fact]
    public async Task List_filtra_por_compania_y_pagina()
    {
        // SC-PROVEEDORES-28 a SC-PROVEEDORES-31.
        var repository = new Mock<ISupplierRepository>();
        SupplierFilter? capturedFilter = null;
        repository
            .Setup(repo => repo.ListAsync(
                It.IsAny<SupplierFilter>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Callback<SupplierFilter, int, int, CancellationToken>((filter, _, _, _) => capturedFilter = filter)
            .ReturnsAsync(new[] { CreateSupplier() });
        repository
            .Setup(repo => repo.CountAsync(It.IsAny<SupplierFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);

        var handler = new GetSuppliersQueryHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            new ListingQueryPipeline(new BudgetCivil.Application.Common.Pagination.PaginationValidator()));

        var response = await handler.ExecuteAsync(
            ActorId,
            null,
            new GetSuppliersQuery
            {
                BusinessName = "cemento",
                ContactName = "Ana",
                MunicipalityId = MedellinId,
                Page = 1,
                PageSize = 10
            });

        response.Items.Should().HaveCount(1);
        response.TotalItems.Should().Be(12);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(10);
        capturedFilter!.CompanyId.Should().Be(CompanyId);
        capturedFilter.BusinessName.Should().Be("cemento");
        capturedFilter.ContactName.Should().Be("Ana");
        capturedFilter.MunicipalityId.Should().Be(MedellinId);
    }

    [Fact]
    public async Task List_rechaza_un_tamano_de_pagina_no_permitido()
    {
        var handler = new GetSuppliersQueryHandler(
            AccessService(CompanyId, "DIRECTOR"),
            new Mock<ISupplierRepository>().Object,
            new ListingQueryPipeline(new BudgetCivil.Application.Common.Pagination.PaginationValidator()));

        var action = () => handler.ExecuteAsync(
            ActorId,
            null,
            new GetSuppliersQuery { Page = 1, PageSize = 7 });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("PAGE_SIZE_INVALID");
    }

    [Fact]
    public async Task Options_devuelve_solo_identificador_y_razon_social_activos()
    {
        // SC-PROVEEDORES-32 a SC-PROVEEDORES-34.
        var repository = new Mock<ISupplierRepository>();
        repository
            .Setup(repo => repo.ListActiveOptionsAsync(
                It.IsAny<SupplierFilter>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateSupplier() });

        var handler = new GetSupplierOptionsQueryHandler(
            AccessService(CompanyId, "AUDITOR"),
            repository.Object);

        var response = await handler.ExecuteAsync(
            ActorId,
            null,
            new GetSupplierOptionsQuery { BusinessName = "cemento", CountryId = ColombiaId });

        response.Items.Should().HaveCount(1);
        response.Items[0].SupplierId.Should().Be(SupplierId);
        response.Items[0].BusinessName.Should().Be("Cementos del Norte");
        response.Items[0].Should().BeOfType<SupplierOptionDto>();
        typeof(SupplierOptionDto).GetProperties().Should().HaveCount(2);
    }

    [Fact]
    public void Alert_responde_coincidencia_en_el_mismo_municipio()
    {
        // SC-PROVEEDORES-35.
        var alert = GetSupplierLocationAlertQueryHandler.Compare(
            SupplierId,
            Guid.NewGuid(),
            SupplierLocationDto(MedellinId),
            ProjectLocation(MedellinId));

        alert.CanCompare.Should().BeTrue();
        alert.IsSameLocation.Should().BeTrue();
        alert.Code.Should().Be("SUPPLIER_LOCATION_MATCH");
    }

    [Fact]
    public void Alert_responde_discrepancia_en_municipios_distintos()
    {
        // SC-PROVEEDORES-36.
        var alert = GetSupplierLocationAlertQueryHandler.Compare(
            SupplierId,
            Guid.NewGuid(),
            SupplierLocationDto(MedellinId),
            ProjectLocation(BogotaId));

        alert.CanCompare.Should().BeTrue();
        alert.IsSameLocation.Should().BeFalse();
        alert.Code.Should().Be("SUPPLIER_LOCATION_MISMATCH");
        alert.Message.Should().Contain("Medellín").And.Contain("Bogotá");
    }

    [Fact]
    public void Alert_compara_por_departamento_cuando_no_hay_municipio()
    {
        // SC-PROVEEDORES-37.
        var supplierLocation = SupplierLocationDto(municipalityId: null);
        var projectLocation = new SupplierLocation(ColombiaId, "Colombia", AntioquiaId, "Antioquia", BogotaId, "Bogotá");

        var alert = GetSupplierLocationAlertQueryHandler.Compare(
            SupplierId,
            Guid.NewGuid(),
            supplierLocation,
            projectLocation);

        alert.CanCompare.Should().BeTrue();
        alert.IsSameLocation.Should().BeTrue();
        alert.Code.Should().Be("SUPPLIER_LOCATION_MATCH");
    }

    [Fact]
    public void Alert_informa_ubicacion_incompleta()
    {
        // SC-PROVEEDORES-38.
        var supplierLocation = new LocationDto();
        var projectLocation = new SupplierLocation(ColombiaId, "Colombia", AntioquiaId, "Antioquia", MedellinId, "Medellín");

        var alert = GetSupplierLocationAlertQueryHandler.Compare(
            SupplierId,
            Guid.NewGuid(),
            supplierLocation,
            projectLocation);

        alert.CanCompare.Should().BeFalse();
        alert.Code.Should().Be("LOCATION_INCOMPLETE");
    }

    [Fact]
    public async Task Alert_rechaza_un_proyecto_de_otra_compania()
    {
        // SC-PROVEEDORES-39 (la alerta no bloquea) y aislamiento por compañía.
        var repository = new Mock<ISupplierRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(SupplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSupplier());

        var projectAccessReader = new Mock<BudgetCivil.Application.Resources.Repositories.IResourceProjectAccessReader>();
        projectAccessReader
            .Setup(reader => reader.GetAccessAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetCivil.Application.Resources.Repositories.ResourceProjectAccess(
                Guid.NewGuid(),
                OtherCompanyId,
                IsMember: false));

        var handler = new GetSupplierLocationAlertQueryHandler(
            AccessService(CompanyId, "DIRECTOR"),
            repository.Object,
            projectAccessReader.Object);

        var action = () => handler.ExecuteAsync(
            ActorId,
            SupplierId,
            new GetSupplierLocationAlertQuery { ProjectId = Guid.NewGuid() });

        var exception = await action.Should().ThrowAsync<NotFoundAppException>();
        exception.Which.Code.Should().Be("PROJECT_NOT_FOUND");
    }

    [Theory]
    [InlineData("ADMINISTRADOR", false)]
    [InlineData("AUDITOR", false)]
    public async Task Los_roles_de_solo_lectura_no_escriben(string roleCode, bool expectedWrite)
    {
        // SC-PROVEEDORES-24 y SC-PROVEEDORES-25.
        var handler = new CreateSupplierCommandHandler(
            AccessService(CompanyId, roleCode),
            new Mock<ISupplierRepository>().Object,
            new CreateSupplierCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            new CreateSupplierCommand { BusinessName = "Cementos del Norte" });

        if (expectedWrite)
        {
            await action.Should().NotThrowAsync();
            return;
        }

        var exception = await action.Should().ThrowAsync<ForbiddenAppException>();
        exception.Which.Code.Should().Be("SUPPLIER_WRITE_FORBIDDEN");
    }

    [Theory]
    [InlineData("ADMINISTRADOR", null)]
    [InlineData("AUDITOR", null)]
    [InlineData("DIRECTOR", null)]
    [InlineData("PRESUPUESTADOR", null)]
    public async Task Los_cuatro_roles_consultan_el_listado(string roleCode, Guid? companyFilter)
    {
        // SC-PROVEEDORES-26 y SC-PROVEEDORES-27.
        var repository = new Mock<ISupplierRepository>();
        repository
            .Setup(repo => repo.ListAsync(
                It.IsAny<SupplierFilter>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository
            .Setup(repo => repo.CountAsync(It.IsAny<SupplierFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = new GetSuppliersQueryHandler(
            AccessService(CompanyId, roleCode),
            repository.Object,
            new ListingQueryPipeline(new BudgetCivil.Application.Common.Pagination.PaginationValidator()));

        var response = await handler.ExecuteAsync(ActorId, companyFilter, new GetSuppliersQuery());

        response.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_rol_no_autorizado_no_consulta_otra_compania()
    {
        var handler = new GetSuppliersQueryHandler(
            AccessService(CompanyId, "DIRECTOR"),
            new Mock<ISupplierRepository>().Object,
            new ListingQueryPipeline(new BudgetCivil.Application.Common.Pagination.PaginationValidator()));

        var action = () => handler.ExecuteAsync(
            ActorId,
            OtherCompanyId,
            new GetSuppliersQuery());

        var exception = await action.Should().ThrowAsync<ForbiddenAppException>();
        exception.Which.Code.Should().Be("SUPPLIER_READ_FORBIDDEN");
    }

    [Fact]
    public async Task El_administrador_consulta_cualquier_compania()
    {
        // SC-PROVEEDORES-26.
        var repository = new Mock<ISupplierRepository>();
        repository
            .Setup(repo => repo.ListAsync(
                It.IsAny<SupplierFilter>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository
            .Setup(repo => repo.CountAsync(It.IsAny<SupplierFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = new GetSuppliersQueryHandler(
            AccessService(CompanyId, "ADMINISTRADOR"),
            repository.Object,
            new ListingQueryPipeline(new BudgetCivil.Application.Common.Pagination.PaginationValidator()));

        await handler.ExecuteAsync(ActorId, OtherCompanyId, new GetSuppliersQuery());

        repository.Verify(
            repo => repo.CountAsync(
                It.Is<SupplierFilter>(filter => filter.CompanyId == OtherCompanyId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static LocationDto SupplierLocationDto(Guid? municipalityId)
    {
        return new LocationDto
        {
            CountryId = ColombiaId,
            CountryName = "Colombia",
            DepartmentId = AntioquiaId,
            DepartmentName = "Antioquia",
            MunicipalityId = municipalityId,
            MunicipalityName = municipalityId.HasValue ? "Medellín" : null
        };
    }

    private static SupplierLocation ProjectLocation(Guid municipalityId)
    {
        return new SupplierLocation(
            ColombiaId,
            "Colombia",
            AntioquiaId,
            "Antioquia",
            municipalityId,
            municipalityId == MedellinId ? "Medellín" : "Bogotá");
    }

    private static SupplierAccessService AccessService(Guid companyId, string roleCode)
    {
        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActorAccessSnapshot(
                ActorId,
                companyId,
                Guid.NewGuid(),
                roleCode,
                AccountEnabled: true,
                AccountLocked: false,
                RoleIsActive: true,
                CompanyIsActive: true,
                GrantedPermissionKeys: []));

        return new SupplierAccessService(actorReader.Object);
    }

    private static Supplier CreateSupplier()
    {
        var supplier = Supplier.Create(
            CompanyId,
            "Cementos del Norte",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            ActorId,
            DateTime.UtcNow);

        supplier.Id = SupplierId;
        return supplier;
    }

    private static void SetupAddAndReload(Mock<ISupplierRepository> repository, Action<Supplier> onCreated)
    {
        Supplier? stored = null;
        repository
            .Setup(repo => repo.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .Callback<Supplier, CancellationToken>((supplier, _) =>
            {
                stored = supplier;
                onCreated(supplier);
            })
            .Returns(Task.CompletedTask);
        repository
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);
    }

    private static void SetupLocation(
        Mock<ISupplierRepository> repository,
        Guid? departmentCountryId = null,
        Guid? municipalityDepartmentId = null,
        bool municipalityExists = true)
    {
        repository
            .Setup(repo => repo.GetLocationHierarchyAsync(
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocationHierarchy(
                departmentCountryId,
                municipalityDepartmentId,
                CountryExists: true,
                DepartmentExists: true,
                MunicipalityExists: municipalityExists));
    }
}
