using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Pagination;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Resources.Commands.CopyResource;
using BudgetCivil.Application.Resources.Commands.CreateResource;
using BudgetCivil.Application.Resources.Commands.UpdateResource;
using BudgetCivil.Application.Resources.Queries;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Resources.Services;
using BudgetCivil.Application.Suppliers.Repositories;
using BudgetCivil.Domain.Resources;
using BudgetCivil.Domain.Suppliers;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

public sealed class ResourceHandlersTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid TargetProjectId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ResourceTypeId = Guid.NewGuid();
    private static readonly Guid MunicipalityId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();

    [Fact]
    public async Task Create_crea_el_insumo_con_estado_autor_y_tipo()
    {
        var repository = new Mock<IResourceRepository>();
        Resource? created = null;
        SetupReferences(repository);
        repository
            .Setup(repo => repo.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .Callback<Resource, CancellationToken>((resource, _) => created = resource)
            .Returns(Task.CompletedTask);
        repository
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(repo => repo.GetByIdAsync(ProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => AttachType(created));

        var handler = new CreateResourceCommandHandler(
            AccessService(Actor("DIRECTOR", CompanyId), CompanyId, isMember: false),
            repository.Object,
            ActiveSupplierRepository(),
            new CreateResourceCommandValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new CreateResourceCommand
            {
                ResourceTypeId = ResourceTypeId,
                MunicipalityId = MunicipalityId,
                Description = "Cemento gris",
                Unit = "m2",
                UnitPrice = 10m,
                Brand = "Marca",
                SupplierId = SupplierId
            });

        response.QuotationStatus.Should().Be(QuotationStatusCodes.PorCotizar);
        response.LastModifiedBy.Should().Be(ActorId);
        response.ResourceTypeId.Should().Be(ResourceTypeId);
        response.OriginResourceId.Should().BeNull();
        response.SupplierId.Should().Be(SupplierId);
        created.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_rechaza_un_tipo_inexistente()
    {
        var repository = new Mock<IResourceRepository>();
        repository
            .Setup(repo => repo.ResourceTypeExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CreateResourceCommandHandler(
            AccessService(Actor("DIRECTOR", CompanyId), CompanyId, isMember: false),
            repository.Object,
            ActiveSupplierRepository(),
            new CreateResourceCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new CreateResourceCommand
            {
                ResourceTypeId = ResourceTypeId,
                MunicipalityId = MunicipalityId,
                Description = "Cemento gris"
            });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("RESOURCE_TYPE_INVALID");
    }

    [Fact]
    public async Task Update_aplica_autoría_y_nueva_version()
    {
        var resource = CreateResource();
        resource.RowVersion = [1, 2, 3];
        var oldAuthor = resource.LastModifiedByUserId;
        var repository = new Mock<IResourceRepository>();
        SetupReferences(repository);
        repository
            .Setup(repo => repo.GetByIdAsync(ProjectId, resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);
        repository
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateResourceCommandHandler(
            AccessService(Actor("DIRECTOR", CompanyId), CompanyId, isMember: false),
            repository.Object,
            ActiveSupplierRepository(),
            new UpdateResourceCommandValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            resource.Id,
            new UpdateResourceCommand
            {
                ResourceTypeId = ResourceTypeId,
                MunicipalityId = MunicipalityId,
                Description = "Cemento gris actualizado",
                Unit = "m2",
                UnitPrice = 12m,
                Brand = "Marca",
                SupplierId = SupplierId,
                RowVersion = Convert.ToBase64String(resource.RowVersion)
            });

        response.Description.Should().Be("Cemento gris actualizado");
        resource.LastModifiedByUserId.Should().NotBe(oldAuthor);
        resource.LastModifiedByUserId.Should().Be(ActorId);
    }

    [Fact]
    public async Task Update_con_version_desactualizada_devuelve_conflicto()
    {
        var resource = CreateResource();
        resource.RowVersion = [1, 2, 3];
        var repository = new Mock<IResourceRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(ProjectId, resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);

        var handler = new UpdateResourceCommandHandler(
            AccessService(Actor("DIRECTOR", CompanyId), CompanyId, isMember: false),
            repository.Object,
            ActiveSupplierRepository(),
            new UpdateResourceCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            ProjectId,
            resource.Id,
            new UpdateResourceCommand
            {
                ResourceTypeId = ResourceTypeId,
                MunicipalityId = MunicipalityId,
                Description = "Cemento gris",
                RowVersion = Convert.ToBase64String([9, 9, 9])
            });

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be("RESOURCE_CONCURRENCY_CONFLICT");
    }

    [Fact]
    public async Task Copy_preserva_datos_y_asigna_procedencia()
    {
        var source = CreateResource();
        source.OriginResourceId = null;
        var repository = new Mock<IResourceRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        Resource? copy = null;
        repository
            .Setup(repo => repo.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()))
            .Callback<Resource, CancellationToken>((resource, _) => copy = resource)
            .Returns(Task.CompletedTask);
        repository
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository
            .Setup(repo => repo.GetByIdAsync(TargetProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => AttachType(copy));

        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Actor("DIRECTOR", CompanyId));

        var projectReader = new Mock<IResourceProjectAccessReader>();
        projectReader
            .Setup(reader => reader.GetAccessAsync(TargetProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceProjectAccess(TargetProjectId, CompanyId, false));
        projectReader
            .Setup(reader => reader.GetAccessAsync(ProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceProjectAccess(ProjectId, CompanyId, false));

        var handler = new CopyResourceCommandHandler(
            new ResourceAccessService(actorReader.Object, projectReader.Object),
            repository.Object,
            ActiveSupplierRepository(),
            new CopyResourceCommandValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            TargetProjectId,
            new CopyResourceCommand { SourceResourceId = source.Id });

        response.ProjectId.Should().Be(TargetProjectId);
        response.OriginResourceId.Should().Be(source.Id);
        copy.Should().NotBeNull();
        copy!.ProjectId.Should().Be(TargetProjectId);
        source.ProjectId.Should().Be(ProjectId);
        source.UnitPrice.Should().Be(copy.UnitPrice);
    }

    [Fact]
    public async Task List_aplica_paginacion_y_devuelve_el_total()
    {
        var repository = new Mock<IResourceRepository>();
        repository
            .Setup(repo => repo.ListAsync(
                It.IsAny<ResourceFilter>(),
                It.IsAny<ResourceSort>(),
                1,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateResource() });
        repository
            .Setup(repo => repo.CountAsync(It.IsAny<ResourceFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);
        repository
            .Setup(repo => repo.ResourceTypeExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository
            .Setup(repo => repo.MunicipalityExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new GetResourcesQueryHandler(
            AccessService(Actor("DIRECTOR", CompanyId), CompanyId, isMember: false),
            repository.Object,
            new ListingQueryPipeline(new PaginationValidator()));

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new GetResourcesQuery { Page = 1, PageSize = 10 });

        response.Items.Should().HaveCount(1);
        response.TotalItems.Should().Be(12);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Suggestions_devuelve_los_insumos_similares()
    {
        var repository = new Mock<IResourceRepository>();
        repository
            .Setup(repo => repo.GetSimilarAsync(
                ProjectId,
                "cemento",
                null,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateResource() });

        var handler = new GetResourceSuggestionsQueryHandler(
            AccessService(Actor("PRESUPUESTADOR", CompanyId), CompanyId, isMember: true),
            repository.Object,
            new GetResourceSuggestionsQueryValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new GetResourceSuggestionsQuery { Description = "cemento" });

        response.Items.Should().HaveCount(1);
        response.Items[0].Description.Should().Be("Cemento gris");
    }

    [Fact]
    public async Task Copy_rechaza_un_proveedor_de_otra_compania()
    {
        // SC-PROVEEDORES-48: la copia no se crea si el proveedor no es de la compañía de destino.
        var source = CreateResource();
        var repository = new Mock<IResourceRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        repository
            .Setup(repo => repo.GetByIdAsync(TargetProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => AttachType(source));

        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Actor("DIRECTOR", CompanyId));

        var projectReader = new Mock<IResourceProjectAccessReader>();
        projectReader
            .Setup(reader => reader.GetAccessAsync(TargetProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceProjectAccess(TargetProjectId, CompanyId, false));
        projectReader
            .Setup(reader => reader.GetAccessAsync(ProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceProjectAccess(ProjectId, CompanyId, false));

        var supplierRepository = new Mock<ISupplierRepository>();
        supplierRepository
            .Setup(repo => repo.BelongsToCompanyAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = new CopyResourceCommandHandler(
            new ResourceAccessService(actorReader.Object, projectReader.Object),
            repository.Object,
            supplierRepository.Object,
            new CopyResourceCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            TargetProjectId,
            new CopyResourceCommand { SourceResourceId = source.Id });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("SUPPLIER_COMPANY_MISMATCH");
        repository.Verify(
            repo => repo.AddAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_rechaza_un_proveedor_inactivo()
    {
        // SC-PROVEEDORES-42: no se permite una nueva asociación con un proveedor inactivo.
        var repository = new Mock<IResourceRepository>();
        SetupReferences(repository);

        var supplierRepository = new Mock<ISupplierRepository>();
        supplierRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Supplier
            {
                Id = SupplierId,
                CompanyId = CompanyId,
                BusinessName = "Cementos del Norte",
                IsActive = false
            });

        var handler = new CreateResourceCommandHandler(
            AccessService(Actor("DIRECTOR", CompanyId), CompanyId, isMember: false),
            repository.Object,
            supplierRepository.Object,
            new CreateResourceCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new CreateResourceCommand
            {
                ResourceTypeId = ResourceTypeId,
                MunicipalityId = MunicipalityId,
                Description = "Cemento gris",
                SupplierId = SupplierId
            });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("SUPPLIER_INACTIVE");
    }

    [Fact]
    public async Task Create_rechaza_un_proveedor_de_otra_compania()
    {
        var repository = new Mock<IResourceRepository>();
        SetupReferences(repository);

        var supplierRepository = new Mock<ISupplierRepository>();
        supplierRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Supplier
            {
                Id = SupplierId,
                CompanyId = Guid.NewGuid(),
                BusinessName = "Cementos del Norte",
                IsActive = true
            });

        var handler = new CreateResourceCommandHandler(
            AccessService(Actor("DIRECTOR", CompanyId), CompanyId, isMember: false),
            repository.Object,
            supplierRepository.Object,
            new CreateResourceCommandValidator());

        var action = () => handler.ExecuteAsync(
            ActorId,
            ProjectId,
            new CreateResourceCommand
            {
                ResourceTypeId = ResourceTypeId,
                MunicipalityId = MunicipalityId,
                Description = "Cemento gris",
                SupplierId = SupplierId
            });

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("SUPPLIER_COMPANY_MISMATCH");
    }

    [Fact]
    public async Task Update_conserva_el_proveedor_inactivo_ya_asociado()
    {
        // D5: una edición de otros campos no falla porque el proveedor ya asociado esté inactivo.
        var resource = CreateResource();
        resource.RowVersion = [1, 2, 3];
        var repository = new Mock<IResourceRepository>();
        SetupReferences(repository);
        repository
            .Setup(repo => repo.GetByIdAsync(ProjectId, resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);
        repository
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var supplierRepository = new Mock<ISupplierRepository>();

        var handler = new UpdateResourceCommandHandler(
            AccessService(Actor("DIRECTOR", CompanyId), CompanyId, isMember: false),
            repository.Object,
            supplierRepository.Object,
            new UpdateResourceCommandValidator());

        var response = await handler.ExecuteAsync(
            ActorId,
            ProjectId,
            resource.Id,
            new UpdateResourceCommand
            {
                ResourceTypeId = ResourceTypeId,
                MunicipalityId = MunicipalityId,
                Description = "Cemento gris",
                Unit = "m2",
                UnitPrice = 10m,
                Brand = "Marca",
                SupplierId = SupplierId,
                RowVersion = Convert.ToBase64String(resource.RowVersion)
            });

        response.SupplierId.Should().Be(SupplierId);
        supplierRepository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static void SetupReferences(Mock<IResourceRepository> repository)
    {
        repository
            .Setup(repo => repo.ResourceTypeExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repository
            .Setup(repo => repo.MunicipalityExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private static ResourceAccessService AccessService(
        ActorAccessSnapshot actor,
        Guid projectCompanyId,
        bool isMember)
    {
        var actorReader = new Mock<IActorAccessReader>();
        actorReader
            .Setup(reader => reader.GetSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(actor);

        var projectReader = new Mock<IResourceProjectAccessReader>();
        projectReader
            .Setup(reader => reader.GetAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResourceProjectAccess(ProjectId, projectCompanyId, isMember));

        return new ResourceAccessService(actorReader.Object, projectReader.Object);
    }

    private static ActorAccessSnapshot Actor(string roleCode, Guid? companyId)
    {
        return new ActorAccessSnapshot(
            ActorId,
            companyId,
            Guid.NewGuid(),
            roleCode,
            AccountEnabled: true,
            AccountLocked: false,
            RoleIsActive: true,
            CompanyIsActive: companyId.HasValue ? true : null,
            GrantedPermissionKeys: []);
    }

    private static Resource CreateResource()
    {
        var resource = Resource.Create(
            ProjectId,
            ResourceTypeId,
            MunicipalityId,
            "Cemento gris",
            "m2",
            10m,
            "Marca",
            null,
            null,
            null,
            null,
            SupplierId,
            Guid.NewGuid(),
            DateTime.UtcNow);

        return AttachType(resource);
    }

    /// <summary>Proveedor activo de la misma compañía del proyecto (`REQ-PROVEEDORES-04`).</summary>
    private static ISupplierRepository ActiveSupplierRepository()
    {
        var supplierRepository = new Mock<ISupplierRepository>();
        supplierRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Supplier
            {
                Id = SupplierId,
                CompanyId = CompanyId,
                BusinessName = "Cementos del Norte",
                IsActive = true
            });
        supplierRepository
            .Setup(repo => repo.BelongsToCompanyAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return supplierRepository.Object;
    }

    private static Resource AttachType(Resource? resource)
    {
        if (resource is null)
        {
            throw new InvalidOperationException("Resource was not created.");
        }

        resource.ResourceType = new ResourceType
        {
            Id = resource.ResourceTypeId,
            Code = ResourceTypeCatalog.Material,
            Name = "Materiales",
            IsActive = true
        };

        return resource;
    }
}
