using System.Text.Json;
using BudgetCivil.Application.Resources.Dtos;
using BudgetCivil.Application.Suppliers.Dtos;
using BudgetCivil.Domain.Resources;
using BudgetCivil.Domain.Suppliers;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

/// <summary>
/// `T-C-03`: contrato v1 de proveedores y del resumen dentro del insumo (`D-41`, `D-55`).
/// </summary>
public sealed class SupplierContractTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void El_detalle_usa_camel_case_y_expone_los_campos_del_contrato()
    {
        var json = JsonSerializer.Serialize(SupplierMapper.ToDto(CreateSupplier()), WebOptions);

        json.Should().Contain("\"supplierId\"");
        json.Should().Contain("\"companyId\"");
        json.Should().Contain("\"businessName\"");
        json.Should().Contain("\"contactName\"");
        json.Should().Contain("\"contactPhone\"");
        json.Should().Contain("\"email\"");
        json.Should().Contain("\"address\"");
        json.Should().Contain("\"observations\"");
        json.Should().Contain("\"countryId\"");
        json.Should().Contain("\"departmentId\"");
        json.Should().Contain("\"municipalityId\"");
        json.Should().Contain("\"isActive\"");
        json.Should().Contain("\"rowVersion\"");
        json.Should().Contain("\"lastModifiedBy\"");
        json.Should().Contain("\"lastModifiedAt\"");
        json.Should().NotContain("supplier_id");
        json.Should().NotContain("\"SupplierId\"");
    }

    [Fact]
    public void El_selector_solo_expone_identificador_y_razon_social()
    {
        // SC-PROVEEDORES-32: el selector no expone contacto ni ubicación.
        var option = SupplierMapper.ToOptionDto(CreateSupplier());
        var json = JsonSerializer.Serialize(option, WebOptions);

        json.Should().Contain("\"supplierId\"");
        json.Should().Contain("\"businessName\"");
        json.Should().NotContain("contactName");
        json.Should().NotContain("contactPhone");
        json.Should().NotContain("email");
        json.Should().NotContain("address");
        json.Should().NotContain("observations");
        json.Should().NotContain("countryId");
        json.Should().NotContain("departmentId");
        json.Should().NotContain("municipalityId");
        typeof(SupplierOptionDto).GetProperties().Select(property => property.Name)
            .Should().BeEquivalentTo(["SupplierId", "BusinessName"]);
    }

    [Fact]
    public void La_alerta_expone_canCompare_code_y_ambas_ubicaciones()
    {
        var alert = new LocationAlertDto
        {
            SupplierId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            CanCompare = true,
            IsSameLocation = false,
            Code = LocationAlertCodes.Mismatch,
            Message = "El proveedor está en Medellín, Antioquia, Colombia.",
            SupplierLocation = new LocationDto { MunicipalityId = Guid.NewGuid() },
            ProjectLocation = new LocationDto { MunicipalityId = Guid.NewGuid() }
        };

        var json = JsonSerializer.Serialize(alert, WebOptions);

        json.Should().Contain("\"supplierId\"");
        json.Should().Contain("\"projectId\"");
        json.Should().Contain("\"canCompare\"");
        json.Should().Contain("\"isSameLocation\"");
        json.Should().Contain("\"code\":\"SUPPLIER_LOCATION_MISMATCH\"");
        json.Should().Contain("\"message\"");
        json.Should().Contain("\"supplierLocation\"");
        json.Should().Contain("\"projectLocation\"");
    }

    [Fact]
    public void El_insumo_expone_el_resumen_del_proveedor()
    {
        // SC-PROVEEDORES-49: razón social, contacto, dirección y ubicación del proveedor.
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
            Guid.NewGuid(),
            DateTime.UtcNow);

        resource.Supplier = CreateSupplier();

        var json = JsonSerializer.Serialize(ResourceMapper.ToDto(resource), WebOptions);

        json.Should().Contain("\"supplierId\"");
        json.Should().Contain("\"supplier\":");
        json.Should().Contain("\"businessName\"");
        json.Should().Contain("\"contactName\"");
        json.Should().Contain("\"email\"");
        json.Should().Contain("\"address\"");
        json.Should().Contain("\"municipalityId\"");
        json.Should().Contain("\"distributor\"");
        json.Should().Contain("\"contactNumber\"");
        json.Should().NotContain("\"observations\":\"Notas\"");
    }

    [Fact]
    public void El_insumo_sin_proveedor_devuelve_el_resumen_nulo()
    {
        // SC-PROVEEDORES-50: sin proveedor, los campos heredados siguen disponibles.
        var resource = Resource.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Cemento gris",
            "m2",
            10m,
            "Marca",
            "Distribuidor heredado",
            null,
            null,
            "3001234567",
            null,
            Guid.NewGuid(),
            DateTime.UtcNow);

        var dto = ResourceMapper.ToDto(resource);

        dto.Supplier.Should().BeNull();
        dto.SupplierId.Should().BeNull();
        dto.Distributor.Should().Be("Distribuidor heredado");
        dto.ContactNumber.Should().Be("3001234567");
        dto.QuotationStatus.Should().Be(QuotationStatusCodes.FaltaInformacion);
    }

    private static Supplier CreateSupplier()
    {
        var supplier = Supplier.Create(
            Guid.NewGuid(),
            "Cementos del Norte",
            "Ana Pérez",
            "3001234567",
            "contacto@cementos.example.com",
            "Calle 10 #20-30",
            "Notas",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        supplier.RowVersion = [1, 2, 3];
        supplier.Country = new BudgetCivil.Domain.Catalogs.Country { Id = Guid.NewGuid(), Name = "Colombia" };
        supplier.Department = new BudgetCivil.Domain.Catalogs.Department { Id = Guid.NewGuid(), Name = "Antioquia" };
        supplier.Municipality = new BudgetCivil.Domain.Catalogs.Municipality { Id = Guid.NewGuid(), Name = "Medellín" };

        return supplier;
    }
}
