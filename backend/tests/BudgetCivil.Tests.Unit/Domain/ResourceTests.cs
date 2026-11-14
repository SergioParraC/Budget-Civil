using BudgetCivil.Domain.Resources;
using BudgetCivil.Domain.Suppliers;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Domain;

/// <summary>
/// `T-U-15`/`T-U-16`: campos, ubicación, estado del insumo y copia con proveedor
/// (`SC-PROVEEDORES-13`…`SC-PROVEEDORES-19`, `SC-PROVEEDORES-47`, `SC-PROVEEDORES-48`).
/// </summary>
public sealed class ResourceTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid TargetProjectId = Guid.NewGuid();
    private static readonly Guid ResourceTypeId = Guid.NewGuid();
    private static readonly Guid MunicipalityId = Guid.NewGuid();
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_con_los_cinco_campos_obligatorios_queda_por_cotizar()
    {
        var resource = CreateComplete();

        resource.QuotationStatus.Should().Be(QuotationStatus.PorCotizar);
        resource.ProjectId.Should().Be(ProjectId);
        resource.LastModifiedByUserId.Should().Be(ActorId);
        resource.LastModifiedAtUtc.Should().Be(Now);
        resource.IsActive.Should().BeTrue();
        resource.OriginResourceId.Should().BeNull();
        resource.SupplierId.Should().Be(SupplierId);
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData("m2", null)]
    [InlineData("m2", 10)]
    public void Create_incompleto_queda_falta_informacion(string? unit, int? unitPrice)
    {
        decimal? normalizedUnitPrice = unitPrice;
        var resource = Resource.Create(
            ProjectId,
            ResourceTypeId,
            MunicipalityId,
            "Cemento gris",
            unit,
            normalizedUnitPrice,
            brand: null,
            distributor: null,
            userKey: null,
            details: null,
            contactNumber: null,
            supplierId: SupplierId,
            ActorId,
            Now);

        resource.QuotationStatus.Should().Be(QuotationStatus.FaltaInformacion);
    }

    [Fact]
    public void Create_sin_proveedor_queda_falta_informacion_aunque_este_completo()
    {
        // SC-PROVEEDORES-14 y SC-PROVEEDORES-17: el insumo sin proveedor se guarda y queda incompleto.
        var resource = Resource.Create(
            ProjectId,
            ResourceTypeId,
            MunicipalityId,
            "Cemento gris",
            "m2",
            10m,
            "Marca",
            distributor: null,
            userKey: null,
            details: null,
            contactNumber: null,
            supplierId: null,
            ActorId,
            Now);

        resource.SupplierId.Should().BeNull();
        resource.IsComplete.Should().BeFalse();
        resource.QuotationStatus.Should().Be(QuotationStatus.FaltaInformacion);
    }

    [Fact]
    public void Create_rechaza_un_proveedor_vacio()
    {
        var action = () => Resource.Create(
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
            Guid.Empty,
            ActorId,
            Now);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Update_completa_el_insumo_y_cambia_por_cotizar()
    {
        // SC-PROVEEDORES-18: asociar proveedor y completar los datos pasa a POR_COTIZAR.
        var resource = Resource.Create(
            ProjectId,
            ResourceTypeId,
            MunicipalityId,
            "Cemento gris",
            unit: null,
            unitPrice: 10m,
            brand: "Marca",
            distributor: null,
            userKey: null,
            details: null,
            contactNumber: null,
            supplierId: SupplierId,
            ActorId,
            Now);

        resource.QuotationStatus.Should().Be(QuotationStatus.FaltaInformacion);

        resource.Update(
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
            Now.AddMinutes(1));

        resource.QuotationStatus.Should().Be(QuotationStatus.PorCotizar);
        resource.Unit.Should().Be("m2");
        resource.ProjectId.Should().Be(ProjectId);
    }

    [Fact]
    public void Update_que_retira_un_campo_obligatorio_vuelve_a_falta_informacion()
    {
        var resource = CreateComplete();

        resource.Update(
            ResourceTypeId,
            MunicipalityId,
            "Cemento gris",
            unit: " ",
            unitPrice: 10m,
            brand: "Marca",
            distributor: null,
            userKey: null,
            details: null,
            contactNumber: null,
            supplierId: SupplierId,
            ActorId,
            Now.AddMinutes(1));

        resource.QuotationStatus.Should().Be(QuotationStatus.FaltaInformacion);
        resource.Unit.Should().BeNull();
    }

    [Fact]
    public void Update_que_retira_el_proveedor_vuelve_a_falta_informacion()
    {
        // SC-PROVEEDORES-19: retirar el proveedor devuelve el insumo a FALTA_INFORMACION.
        var resource = CreateComplete();

        resource.Update(
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
            supplierId: null,
            ActorId,
            Now.AddMinutes(1));

        resource.SupplierId.Should().BeNull();
        resource.QuotationStatus.Should().Be(QuotationStatus.FaltaInformacion);
    }

    [Fact]
    public void Update_puede_reasignar_el_proveedor_sin_afectar_a_los_demas_insumos()
    {
        // SC-PROVEEDORES-15: el insumo pasa al proveedor B y A conserva sus demás insumos.
        var otherSupplierId = Guid.NewGuid();
        var first = CreateComplete();
        var other = CreateComplete();

        first.Update(
            ResourceTypeId,
            MunicipalityId,
            first.Description,
            first.Unit,
            first.UnitPrice,
            first.Brand,
            null,
            null,
            null,
            null,
            otherSupplierId,
            ActorId,
            Now.AddMinutes(1));

        first.SupplierId.Should().Be(otherSupplierId);
        other.SupplierId.Should().Be(SupplierId);
        first.QuotationStatus.Should().Be(QuotationStatus.PorCotizar);
    }

    [Fact]
    public void CopyTo_crea_un_registro_independiente_con_procedencia()
    {
        var source = CreateComplete();
        var copy = source.CopyTo(TargetProjectId, ActorId, Now.AddHours(1));

        copy.Id.Should().NotBe(source.Id);
        copy.ProjectId.Should().Be(TargetProjectId);
        copy.OriginResourceId.Should().Be(source.Id);
        copy.Description.Should().Be(source.Description);
        copy.UnitPrice.Should().Be(source.UnitPrice);
        copy.LastModifiedByUserId.Should().Be(ActorId);
        copy.QuotationStatus.Should().Be(QuotationStatus.PorCotizar);
    }

    [Fact]
    public void CopyTo_conserva_el_proveedor_de_la_misma_compania()
    {
        // SC-PROVEEDORES-47: la copia dentro de la misma compañía conserva supplier_id.
        var source = CreateComplete();

        var copy = source.CopyTo(TargetProjectId, ActorId, Now.AddHours(1));

        copy.SupplierId.Should().Be(SupplierId);
        copy.QuotationStatus.Should().Be(QuotationStatus.PorCotizar);
    }

    [Fact]
    public void La_copia_y_el_origen_no_comparten_el_precio()
    {
        var source = CreateComplete();
        var copy = source.CopyTo(TargetProjectId, ActorId, Now.AddHours(1));

        copy.UnitPrice = 99m;
        source.UnitPrice = 1m;

        copy.UnitPrice.Should().Be(99m);
        source.UnitPrice.Should().Be(1m);
    }

    [Fact]
    public void NormalizeDescription_rechaza_vacia_y_demasiado_larga()
    {
        var empty = () => Resource.NormalizeDescription("   ");
        var tooLong = () => Resource.NormalizeDescription(new string('a', Resource.DescriptionMaxLength + 1));

        empty.Should().Throw<InvalidOperationException>();
        tooLong.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NormalizeUnitPrice_rechaza_negativo()
    {
        var action = () => Resource.NormalizeUnitPrice(-0.01m);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void El_catalogo_tiene_exactamente_siete_tipos_unicos()
    {
        ResourceTypeCatalog.Definitions.Should().HaveCount(7);
        ResourceTypeCatalog.Definitions.Select(definition => definition.Id).Should().OnlyHaveUniqueItems();
        ResourceTypeCatalog.Definitions.Select(definition => definition.Code).Should().OnlyHaveUniqueItems();
        ResourceTypeCatalog.IsKnownCode("matrix").Should().BeTrue();
        ResourceTypeCatalog.IsKnownCode("DESCONOCIDO").Should().BeFalse();
    }

    [Fact]
    public void Un_proveedor_suministra_varios_insumos_del_mismo_proyecto()
    {
        // SC-PROVEEDORES-13: tres insumos guardan el mismo supplier_id sin conflicto.
        var supplier = Supplier.Create(
            Guid.NewGuid(),
            "Ferretería Central",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            ActorId,
            Now);

        var resources = Enumerable.Range(0, 3)
            .Select(index => Resource.Create(
                ProjectId,
                ResourceTypeId,
                MunicipalityId,
                $"Cemento {index}",
                "m2",
                10m,
                "Marca",
                null,
                null,
                null,
                null,
                supplier.Id,
                ActorId,
                Now))
            .ToList();

        resources.Should().OnlyContain(resource => resource.SupplierId == supplier.Id);
        resources.Select(resource => resource.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Un_insumo_referencia_un_solo_proveedor()
    {
        // SC-PROVEEDORES-16: la relación es 1:N con un único proveedor por insumo.
        var resource = CreateComplete();

        resource.SupplierId.Should().Be(SupplierId);

        typeof(Resource).GetProperty(nameof(Resource.SupplierId))!.PropertyType.Should().Be<Guid?>();
    }

    private static Resource CreateComplete()
    {
        return Resource.Create(
            ProjectId,
            ResourceTypeId,
            MunicipalityId,
            "Cemento gris",
            "m2",
            10m,
            "Marca",
            "Distribuidor",
            "CEM-001",
            "Detalle",
            "3001234567",
            SupplierId,
            ActorId,
            Now);
    }
}
