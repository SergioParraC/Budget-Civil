using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Commands.DeleteResource;
using BudgetCivil.Application.Resources.Queries;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Domain.Resources;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-31`/`T-U-32`: consulta por sigla y baja física del insumo.</summary>
public sealed class ResourceUnitAndDeleteHandlersTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ResourceTypeId = Guid.NewGuid();
    private static readonly Guid MunicipalityId = Guid.NewGuid();

    [Fact]
    public async Task La_consulta_por_unidad_entrega_los_insumos_coincidentes()
    {
        var repository = new Mock<IResourceRepository>();
        repository
            .Setup(repo => repo.ListByUnitAsync(ProjectId, "M2", It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateResource("cemento", "m2")]);

        var handler = new GetResourcesByUnitQueryHandler(
            ResourceAccessTestSupport.Create(ProjectId, CompanyId),
            repository.Object);

        var response = await handler.ExecuteAsync(ResourceAccessTestSupport.ActorId, ProjectId, " M2 ");

        response.Should().ContainSingle();
        response[0].Unit.Should().Be("m2");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456789012345678901")]
    public async Task La_consulta_por_unidad_rechaza_una_sigla_invalida(string shortUnit)
    {
        var repository = new Mock<IResourceRepository>();
        var handler = new GetResourcesByUnitQueryHandler(
            ResourceAccessTestSupport.Create(ProjectId, CompanyId),
            repository.Object);

        var action = () => handler.ExecuteAsync(ResourceAccessTestSupport.ActorId, ProjectId, shortUnit);

        var exception = await action.Should().ThrowAsync<ValidationAppException>();
        exception.Which.Code.Should().Be("VALIDATION_ERROR");
        repository.Verify(
            repo => repo.ListByUnitAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task La_baja_fisica_elimina_un_insumo_sin_referencias()
    {
        var resource = CreateResource("cemento", "M2");
        var repository = new Mock<IResourceRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(ProjectId, resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);
        repository
            .Setup(repo => repo.HasReferencesAsync(resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(repo => repo.DeleteAsync(resource, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteResourceCommandHandler(
            ResourceAccessTestSupport.Create(ProjectId, CompanyId),
            repository.Object);

        await handler.ExecuteAsync(ResourceAccessTestSupport.ActorId, ProjectId, resource.Id);

        repository.Verify(
            repo => repo.DeleteAsync(resource, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task La_baja_rechaza_un_insumo_usado_por_apu_matriz_o_copia()
    {
        var resource = CreateResource("cemento", "M2");
        var repository = new Mock<IResourceRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(ProjectId, resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);
        repository
            .Setup(repo => repo.HasReferencesAsync(resource.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeleteResourceCommandHandler(
            ResourceAccessTestSupport.Create(ProjectId, CompanyId),
            repository.Object);

        var action = () => handler.ExecuteAsync(ResourceAccessTestSupport.ActorId, ProjectId, resource.Id);

        var exception = await action.Should().ThrowAsync<ConflictAppException>();
        exception.Which.Code.Should().Be("RESOURCE_IN_USE");
        repository.Verify(
            repo => repo.DeleteAsync(It.IsAny<Resource>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task La_baja_de_un_insumo_inexistente_responde_no_encontrado()
    {
        var repository = new Mock<IResourceRepository>();
        repository
            .Setup(repo => repo.GetByIdAsync(ProjectId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource?)null);

        var handler = new DeleteResourceCommandHandler(
            ResourceAccessTestSupport.Create(ProjectId, CompanyId),
            repository.Object);

        var action = () => handler.ExecuteAsync(
            ResourceAccessTestSupport.ActorId,
            ProjectId,
            Guid.NewGuid());

        var exception = await action.Should().ThrowAsync<NotFoundAppException>();
        exception.Which.Code.Should().Be("RESOURCE_NOT_FOUND");
    }

    private static Resource CreateResource(string description, string unit)
    {
        var resource = Resource.Create(
            ProjectId,
            ResourceTypeId,
            MunicipalityId,
            description,
            unit,
            10m,
            "Marca",
            distributor: null,
            userKey: null,
            details: null,
            contactNumber: null,
            supplierId: null,
            ResourceAccessTestSupport.ActorId,
            DateTime.UtcNow);

        resource.ResourceType = new ResourceType
        {
            Id = ResourceTypeId,
            Code = ResourceTypeCatalog.Material,
            Name = "Materiales",
            IsActive = true
        };

        return resource;
    }
}