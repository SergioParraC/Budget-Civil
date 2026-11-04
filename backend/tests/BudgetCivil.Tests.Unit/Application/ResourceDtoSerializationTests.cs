using System.Text.Json;
using BudgetCivil.Application.Resources.Dtos;
using BudgetCivil.Domain.Resources;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

public sealed class ResourceDtoSerializationTests
{
    [Fact]
    public void El_dto_usa_camel_case_y_expone_los_campos_del_contrato()
    {
        var resource = Resource.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Cemento gris",
            "m2",
            10m,
            "Marca",
            null,
            null,
            null,
            null,
            Guid.NewGuid(),
            DateTime.UtcNow);

        resource.RowVersion = [1, 2, 3];
        resource.ResourceType = new ResourceType
        {
            Id = resource.ResourceTypeId,
            Code = ResourceTypeCatalog.Material,
            Name = "Materiales"
        };

        var json = JsonSerializer.Serialize(
            ResourceMapper.ToDto(resource),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.Should().Contain("\"resourceId\"");
        json.Should().Contain("\"resourceTypeId\"");
        json.Should().Contain("\"quotationStatus\"");
        json.Should().Contain("\"lastModifiedBy\"");
        json.Should().Contain("\"rowVersion\"");
        json.Should().NotContain("\"ResourceId\"");
        json.Should().NotContain("resource_type_id");
    }
}
