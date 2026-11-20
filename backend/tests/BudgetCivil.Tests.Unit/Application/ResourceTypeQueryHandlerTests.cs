using BudgetCivil.Application.Resources.Queries;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Domain.Resources;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-U-29`: tipos activos y orden del catálogo (`REQ-INSUMOS-16`).</summary>
public sealed class ResourceTypeQueryHandlerTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Fact]
    public void El_catalogo_incluye_el_tipo_transporte()
    {
        var transport = ResourceTypeCatalog.Definitions
            .Single(definition => definition.Code == ResourceTypeCatalog.Transport);

        transport.Name.Should().Be("Transporte");
        ResourceTypeCatalog.Definitions.Should().HaveCount(8);
    }

    [Fact]
    public async Task Devuelve_los_tipos_en_el_orden_del_catalogo_y_deja_los_desconocidos_al_final()
    {
        var material = ResourceTypeCatalog.Definitions.Single(definition => definition.Code == ResourceTypeCatalog.Material);
        var matrix = ResourceTypeCatalog.Definitions.Single(definition => definition.Code == ResourceTypeCatalog.Matrix);

        var reader = new Mock<IResourceTypeReader>();
        reader
            .Setup(source => source.ListActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ResourceTypeOption(matrix.Id, matrix.Code, matrix.Name),
                new ResourceTypeOption(Guid.NewGuid(), "OTHER", "Otro"),
                new ResourceTypeOption(material.Id, material.Code, material.Name)
            ]);

        var handler = new GetResourceTypesQueryHandler(
            ResourceAccessTestSupport.Create(ProjectId, CompanyId),
            reader.Object);

        var response = await handler.ExecuteAsync(ResourceAccessTestSupport.ActorId, ProjectId);

        response.Select(type => type.Name).Should().Equal("Materiales", "Matriz", "Otro");
        response.Select(type => type.Code).Should().Equal("MATERIAL", "MATRIX", "OTHER");
    }
}