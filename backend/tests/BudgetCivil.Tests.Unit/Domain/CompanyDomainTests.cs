using BudgetCivil.Domain.Companies;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Domain;

public sealed class CompanyDomainTests
{
    [Fact]
    public void NormalizeBusinessName_recorta_y_rechaza_vacio()
    {
        Company.NormalizeBusinessName("  Constructora ABC  ").Should().Be("Constructora ABC");

        var action = () => Company.NormalizeBusinessName("   ");

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NormalizeTaxId_recorta_y_pasa_a_mayusculas()
    {
        Company.NormalizeTaxId("  nit-900123  ").Should().Be("NIT-900123");

        var action = () => Company.NormalizeTaxId(null!);

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("PUBLICA", "MUNICIPAL")]
    [InlineData("publica", "MUNICIPAL")]
    [InlineData("PRIVADA", null)]
    [InlineData("PRIVADA", "")]
    public void ValidateTerritorialLevel_solo_exige_nivel_a_las_publicas(string companyType, string? territorialLevel)
    {
        var action = () => Company.ValidateTerritorialLevel(companyType, territorialLevel);

        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateTerritorialLevel_rechaza_una_publica_sin_nivel()
    {
        var action = () => Company.ValidateTerritorialLevel("PUBLICA", "  ");

        action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("PUBLICA", "PUBLICA")]
    [InlineData("privada", "PRIVADA")]
    public void NormalizeCompanyType_acepta_los_dos_tipos(string companyType, string expected)
    {
        Company.NormalizeCompanyType(companyType).Should().Be(expected);
    }

    [Theory]
    [InlineData("MIXTA")]
    [InlineData("")]
    [InlineData(null)]
    public void NormalizeCompanyType_rechaza_un_tipo_invalido(string? companyType)
    {
        var action = () => Company.NormalizeCompanyType(companyType);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateRepresentative_acepta_referencia_de_usuario()
    {
        var representative = new LegalRepresentativeInput
        {
            Mode = "USER_REFERENCE",
            UserId = Guid.NewGuid()
        };

        var action = () => Company.ValidateRepresentative(representative);

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidateRepresentative_exige_el_modo(string? mode)
    {
        var action = () => Company.ValidateRepresentative(new LegalRepresentativeInput { Mode = mode, UserId = Guid.NewGuid() });

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateRepresentative_exige_datos_manuales_completos()
    {
        var action = () => Company.ValidateRepresentative(new LegalRepresentativeInput
        {
            Mode = "MANUAL_DATA",
            FullName = "Representante",
            DocumentNumber = "  "
        });

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidateRepresentative_rechaza_mezclar_referencia_y_datos_manuales()
    {
        var withReferenceAndManualData = new LegalRepresentativeInput
        {
            Mode = "USER_REFERENCE",
            UserId = Guid.NewGuid(),
            FullName = "Representante"
        };

        var withManualDataAndUser = new LegalRepresentativeInput
        {
            Mode = "MANUAL_DATA",
            UserId = Guid.NewGuid(),
            FullName = "Representante",
            DocumentNumber = "123"
        };

        var unknownMode = new LegalRepresentativeInput { Mode = "OTRO", UserId = Guid.NewGuid() };

        FluentActions.Invoking(() => Company.ValidateRepresentative(withReferenceAndManualData))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Company.ValidateRepresentative(withManualDataAndUser))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => Company.ValidateRepresentative(unknownMode))
            .Should().Throw<ArgumentException>();
    }
}
