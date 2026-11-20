using System.Text.Json;
using BudgetCivil.Application.Catalogs.Dtos;
using BudgetCivil.Application.Resources.Dtos;
using BudgetCivil.Application.Resources.Queries;
using BudgetCivil.Domain.Resources;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>`T-C-07`/`T-C-08`: contrato camelCase de unidades, tipos e insumos.</summary>
public sealed class UnitContractTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void La_unidad_expone_id_shortUnit_y_unit_en_camel_case()
    {
        var json = JsonSerializer.Serialize(
            new UnitDto { Id = 1, ShortUnit = "M2", Unit = "Metro cuadrado" },
            WebOptions);

        json.Should().Contain("\"id\":1");
        json.Should().Contain("\"shortUnit\":\"M2\"");
        json.Should().Contain("\"unit\":\"Metro cuadrado\"");
        json.Should().NotContain("ShortUnit");
        json.Should().NotContain("short_unit");
    }

    [Fact]
    public void El_tipo_de_insumo_expone_id_y_name()
    {
        var json = JsonSerializer.Serialize(
            new ResourceTypeOptionDto { Id = Guid.NewGuid(), Name = "Materiales" },
            WebOptions);

        json.Should().Contain("\"id\":");
        json.Should().Contain("\"name\":\"Materiales\"");
        json.Should().NotContain("\"code\"");
        json.Should().NotContain("\"isActive\"");
    }

    [Fact]
    public void El_insumo_expone_la_unidad_y_el_estado_de_cotizacion()
    {
        var resource = Resource.Create(
            Guid.NewGuid(),
            ResourceTypeCatalog.Definitions.Single(definition => definition.Code == ResourceTypeCatalog.Material).Id,
            Guid.NewGuid(),
            "Cemento gris",
            "M2",
            10m,
            "Marca",
            distributor: null,
            userKey: null,
            details: null,
            contactNumber: null,
            supplierId: null,
            Guid.NewGuid(),
            DateTime.UtcNow);
        resource.ResourceType = new ResourceType
        {
            Id = resource.ResourceTypeId,
            Code = ResourceTypeCatalog.Material,
            Name = "Materiales",
            IsActive = true
        };

        var json = JsonSerializer.Serialize(ResourceMapper.ToDto(resource), WebOptions);

        json.Should().Contain("\"resourceId\":");
        json.Should().Contain("\"description\":\"Cemento gris\"");
        json.Should().Contain("\"unit\":\"M2\"");
        json.Should().Contain("\"quotationStatus\":");
        json.Should().NotContain("\"ResourceId\"");
    }
}