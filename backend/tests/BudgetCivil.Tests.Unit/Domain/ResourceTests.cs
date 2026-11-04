using BudgetCivil.Domain.Resources;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Domain;

public sealed class ResourceTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid TargetProjectId = Guid.NewGuid();
    private static readonly Guid ResourceTypeId = Guid.NewGuid();
    private static readonly Guid MunicipalityId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_con_los_cuatro_campos_obligatorios_queda_por_cotizar()
    {
        var resource = CreateComplete();

        resource.QuotationStatus.Should().Be(QuotationStatus.PorCotizar);
        resource.ProjectId.Should().Be(ProjectId);
        resource.LastModifiedByUserId.Should().Be(ActorId);
        resource.LastModifiedAtUtc.Should().Be(Now);
        resource.IsActive.Should().BeTrue();
        resource.OriginResourceId.Should().BeNull();
    }

    [Theory]
    [InlineData(null, 10)]
    [InlineData("m2", null)]
    [InlineData("m2", 10)]
    public void Create_incompleto_queda_falta_informacion(string? unit, decimal? unitPrice)
    {
        var resource = Resource.Create(
            ProjectId,
            ResourceTypeId,
            MunicipalityId,
            "Cemento gris",
            unit,
            unitPrice,
            brand: null,
            distributor: null,
            userKey: null,
            details: null,
            contactNumber: null,
            ActorId,
            Now);

        resource.QuotationStatus.Should().Be(QuotationStatus.FaltaInformacion);
    }

    [Fact]
    public void Update_completa_el_insumo_y_cambia_por_cotizar()
    {
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
            ActorId,
            Now.AddMinutes(1));

        resource.QuotationStatus.Should().Be(QuotationStatus.FaltaInformacion);
        resource.Unit.Should().BeNull();
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
            ActorId,
            Now);
    }
}
