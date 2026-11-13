using BudgetCivil.Domain.Suppliers;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Domain;

/// <summary>
/// `T-U-15`: campos opcionales, email, jerarquía de ubicación y duplicados
/// (`SC-PROVEEDORES-04`…`SC-PROVEEDORES-12`, `SC-PROVEEDORES-20`, `SC-PROVEEDORES-21`).
/// </summary>
public sealed class SupplierTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ColombiaId = Guid.NewGuid();
    private static readonly Guid AntioquiaId = Guid.NewGuid();
    private static readonly Guid BogotaId = Guid.NewGuid();
    private static readonly Guid MedellinId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_solo_con_razon_social_deja_los_demas_campos_nulos()
    {
        // SC-PROVEEDORES-04.
        var supplier = CreateMinimal();

        supplier.BusinessName.Should().Be("Cementos del Norte");
        supplier.ContactName.Should().BeNull();
        supplier.ContactPhone.Should().BeNull();
        supplier.Email.Should().BeNull();
        supplier.Address.Should().BeNull();
        supplier.Observations.Should().BeNull();
        supplier.CountryId.Should().BeNull();
        supplier.DepartmentId.Should().BeNull();
        supplier.MunicipalityId.Should().BeNull();
        supplier.IsActive.Should().BeTrue();
        supplier.CompanyId.Should().Be(CompanyId);
    }

    [Fact]
    public void Create_guarda_y_devuelve_todos_los_datos()
    {
        // SC-PROVEEDORES-05 y SC-PROVEEDORES-08.
        var supplier = CreateFull();

        supplier.BusinessName.Should().Be("Cementos del Norte");
        supplier.ContactName.Should().Be("Ana Pérez");
        supplier.ContactPhone.Should().Be("3001234567");
        supplier.Email.Should().Be("contacto@cementos.example.com");
        supplier.Address.Should().Be("Calle 10 #20-30");
        supplier.Observations.Should().Be("Entrega en obra");
        supplier.CountryId.Should().Be(ColombiaId);
        supplier.DepartmentId.Should().Be(AntioquiaId);
        supplier.MunicipalityId.Should().Be(MedellinId);
    }

    [Fact]
    public void Create_registra_autoria_y_fecha()
    {
        // SC-PROVEEDORES-45 (parte de dominio).
        var supplier = CreateMinimal();

        supplier.CreatedByUserId.Should().Be(ActorId);
        supplier.CreatedAtUtc.Should().Be(Now);
        supplier.LastModifiedByUserId.Should().Be(ActorId);
        supplier.LastModifiedAtUtc.Should().Be(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rechaza_la_razon_social_vacia(string businessName)
    {
        // SC-PROVEEDORES-06.
        var action = () => Supplier.Create(
            CompanyId,
            businessName,
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

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_rechaza_la_razon_social_demasiado_larga()
    {
        var action = () => Supplier.NormalizeBusinessName(
            new string('a', Supplier.BusinessNameMaxLength + 1));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_rechaza_la_compania_vacia()
    {
        var action = () => Supplier.Create(
            Guid.Empty,
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
            Now);

        action.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("correo-sin-arroba")]
    [InlineData("correo@sin-dominio")]
    [InlineData("@sin-local.com")]
    public void Create_rechaza_el_correo_invalido(string email)
    {
        // SC-PROVEEDORES-07.
        var action = () => Supplier.NormalizeEmail(email);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NormalizeEmail_admite_nulo_y_recorta_espacios()
    {
        Supplier.NormalizeEmail(null).Should().BeNull();
        Supplier.NormalizeEmail("   ").Should().BeNull();
        Supplier.NormalizeEmail("  contacto@cementos.example.com  ")
            .Should().Be("contacto@cementos.example.com");
    }

    [Fact]
    public void Create_admite_la_ubicacion_completa()
    {
        // SC-PROVEEDORES-09.
        var supplier = CreateFull();

        supplier.CountryId.Should().Be(ColombiaId);
        supplier.DepartmentId.Should().Be(AntioquiaId);
        supplier.MunicipalityId.Should().Be(MedellinId);
    }

    [Fact]
    public void Create_admite_la_ubicacion_parcial()
    {
        // SC-PROVEEDORES-11: solo el país; departamento y municipio quedan nulos.
        var supplier = Supplier.Create(
            CompanyId,
            "Cementos del Norte",
            null,
            null,
            null,
            null,
            null,
            ColombiaId,
            null,
            null,
            ActorId,
            Now);

        supplier.CountryId.Should().Be(ColombiaId);
        supplier.DepartmentId.Should().BeNull();
        supplier.MunicipalityId.Should().BeNull();
    }

    [Fact]
    public void ValidateLocationCoherence_rechaza_un_departamento_de_otro_pais()
    {
        // SC-PROVEEDORES-10.
        var action = () => Supplier.ValidateLocationCoherence(
            ColombiaId,
            AntioquiaId,
            MedellinId,
            departmentCountryId: BogotaId,
            municipalityDepartmentId: AntioquiaId);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateLocationCoherence_rechaza_un_municipio_de_otro_departamento()
    {
        // SC-PROVEEDORES-10.
        var action = () => Supplier.ValidateLocationCoherence(
            ColombiaId,
            AntioquiaId,
            MedellinId,
            departmentCountryId: ColombiaId,
            municipalityDepartmentId: BogotaId);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateLocationCoherence_acepta_la_jerarquia_correcta_y_la_parcial()
    {
        var coherent = () => Supplier.ValidateLocationCoherence(
            ColombiaId,
            AntioquiaId,
            MedellinId,
            departmentCountryId: ColombiaId,
            municipalityDepartmentId: AntioquiaId);

        var partial = () => Supplier.ValidateLocationCoherence(
            ColombiaId,
            null,
            null,
            departmentCountryId: null,
            municipalityDepartmentId: null);

        coherent.Should().NotThrow();
        partial.Should().NotThrow();
    }

    [Fact]
    public void Dos_proveedores_pueden_compartir_la_razon_social()
    {
        // SC-PROVEEDORES-20 y SC-PROVEEDORES-21: no hay unicidad sobre business_name.
        var first = CreateMinimal();
        var second = CreateMinimal();

        first.Id.Should().NotBe(second.Id);
        first.BusinessName.Should().Be(second.BusinessName);
    }

    [Fact]
    public void Update_aplica_campos_y_autoria()
    {
        var supplier = CreateMinimal();
        var editorId = Guid.NewGuid();

        supplier.Update(
            "Cementos del Sur",
            "Luis Gómez",
            "3109876543",
            "ventas@cementos.example.com",
            "Carrera 5 #6-7",
            "Observación",
            ColombiaId,
            AntioquiaId,
            MedellinId,
            editorId,
            Now.AddHours(2));

        supplier.BusinessName.Should().Be("Cementos del Sur");
        supplier.ContactName.Should().Be("Luis Gómez");
        supplier.Email.Should().Be("ventas@cementos.example.com");
        supplier.MunicipalityId.Should().Be(MedellinId);
        supplier.LastModifiedByUserId.Should().Be(editorId);
        supplier.LastModifiedAtUtc.Should().Be(Now.AddHours(2));
        supplier.CreatedByUserId.Should().Be(ActorId);
    }

    [Fact]
    public void Update_rechaza_el_correo_invalido()
    {
        var supplier = CreateMinimal();

        var action = () => supplier.Update(
            "Cementos del Sur",
            null,
            null,
            "correo-invalido",
            null,
            null,
            null,
            null,
            null,
            ActorId,
            Now.AddHours(1));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Update_permite_retirar_la_ubicacion()
    {
        var supplier = CreateFull();

        supplier.Update(
            supplier.BusinessName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            ActorId,
            Now.AddHours(1));

        supplier.CountryId.Should().BeNull();
        supplier.DepartmentId.Should().BeNull();
        supplier.MunicipalityId.Should().BeNull();
    }

    [Fact]
    public void Deactivate_y_Activate_conservan_el_registro()
    {
        // SC-PROVEEDORES-40 y SC-PROVEEDORES-43.
        var supplier = CreateMinimal();
        var editorId = Guid.NewGuid();

        supplier.Deactivate(editorId, Now.AddHours(1));

        supplier.IsActive.Should().BeFalse();
        supplier.LastModifiedByUserId.Should().Be(editorId);

        supplier.Activate(editorId, Now.AddHours(2));

        supplier.IsActive.Should().BeTrue();
        supplier.LastModifiedAtUtc.Should().Be(Now.AddHours(2));
    }

    [Fact]
    public void NormalizeOptional_rechaza_los_campos_demasiado_largos()
    {
        var contactName = () => Supplier.Create(
            CompanyId,
            "Cementos del Norte",
            new string('a', Supplier.ContactNameMaxLength + 1),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            ActorId,
            Now);

        var observations = () => Supplier.Create(
            CompanyId,
            "Cementos del Norte",
            null,
            null,
            null,
            null,
            new string('a', Supplier.ObservationsMaxLength + 1),
            null,
            null,
            null,
            ActorId,
            Now);

        contactName.Should().Throw<InvalidOperationException>();
        observations.Should().Throw<InvalidOperationException>();
    }

    private static Supplier CreateMinimal()
    {
        return Supplier.Create(
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
            Now);
    }

    private static Supplier CreateFull()
    {
        return Supplier.Create(
            CompanyId,
            "Cementos del Norte",
            "Ana Pérez",
            "3001234567",
            "contacto@cementos.example.com",
            "Calle 10 #20-30",
            "Entrega en obra",
            ColombiaId,
            AntioquiaId,
            MedellinId,
            ActorId,
            Now);
    }
}
