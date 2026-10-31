using BudgetCivil.Application.Catalogs.Queries;
using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Pagination;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Domain.Catalogs;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

public sealed class GetCatalogItemsQueryHandlerTests
{
    private readonly Mock<IGeographicCatalogRepository> _repository = new();

    [Fact]
    public async Task Los_paises_se_leen_del_repositorio()
    {
        _repository
            .Setup(repository => repository.GetCountriesAsync(null, 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new List<Country>
                {
                    new() { Id = Guid.NewGuid(), Code = "CO", Name = "Colombia" },
                    new() { Id = Guid.NewGuid(), Code = "PE", Name = "Peru" }
                },
                5));

        var response = await CreateHandler().GetCountriesAsync(new CatalogListingQuery());

        response.Items.Should().HaveCount(2);
        response.Items.Select(item => item.Code).Should().BeEquivalentTo(["CO", "PE"]);
        response.TotalItems.Should().Be(5);
        response.Page.Should().Be(1);

        _repository.Verify(repository => repository.GetCountriesAsync(null, 0, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task La_segunda_pagina_calcula_el_desplazamiento()
    {
        _repository
            .Setup(repository => repository.GetCountriesAsync(null, 10, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Country>(), 5));

        await CreateHandler().GetCountriesAsync(new CatalogListingQuery { Page = 2, PageSize = 10 });

        _repository.Verify(repository => repository.GetCountriesAsync(null, 10, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Los_departamentos_filtran_por_pais()
    {
        var countryId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetDepartmentsAsync(countryId, null, 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new List<Department>
                {
                    new() { Id = Guid.NewGuid(), CountryId = countryId, Code = "CUN", Name = "Cundinamarca" }
                },
                1));

        var response = await CreateHandler().GetDepartmentsAsync(
            new DepartmentCatalogListingQuery { CountryId = countryId });

        response.Items.Should().ContainSingle();
        response.Items[0].CountryId.Should().Be(countryId);
        _repository.Verify(repository => repository.GetDepartmentsAsync(
            countryId, null, 0, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Los_municipios_filtran_por_departamento()
    {
        var departmentId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetMunicipalitiesAsync(departmentId, null, 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new List<Municipality>
                {
                    new() { Id = Guid.NewGuid(), DepartmentId = departmentId, Code = "BOG", Name = "Bogota" }
                },
                1));

        var response = await CreateHandler().GetMunicipalitiesAsync(
            new MunicipalityCatalogListingQuery { DepartmentId = departmentId });

        response.Items.Should().ContainSingle();
        response.Items[0].DepartmentId.Should().Be(departmentId);
    }

    [Fact]
    public async Task Las_monedas_mapean_el_codigo_iso()
    {
        _repository
            .Setup(repository => repository.GetBaseCurrenciesAsync(null, 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new List<BaseCurrency>
                {
                    new() { Id = Guid.NewGuid(), IsoCode = "COP", Name = "Peso Colombiano" }
                },
                1));

        var response = await CreateHandler().GetCurrenciesAsync(new CatalogListingQuery());

        response.Items.Should().ContainSingle();
        response.Items[0].IsoCode.Should().Be("COP");
    }

    [Fact]
    public async Task Los_tipos_de_documento_se_leen_del_repositorio()
    {
        _repository
            .Setup(repository => repository.GetDocumentTypesAsync("CC", 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                new List<DocumentType>
                {
                    new() { Id = Guid.NewGuid(), Code = "CC", Name = "Cedula de ciudadania" }
                },
                1));

        var response = await CreateHandler().GetDocumentTypesAsync(new CatalogListingQuery { Search = "CC" });

        response.Items.Should().ContainSingle();
        response.Items[0].Code.Should().Be("CC");
    }

    [Fact]
    public async Task Un_page_size_no_permitido_falla_antes_de_consultar()
    {
        var action = () => CreateHandler().GetCountriesAsync(new CatalogListingQuery { PageSize = 7 });

        await action.Should().ThrowAsync<ValidationAppException>()
            .Where(exception => exception.Code == "PAGE_SIZE_INVALID");

        _repository.Verify(repository => repository.GetCountriesAsync(
            It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private GetCatalogItemsQueryHandler CreateHandler() => new(
        _repository.Object,
        new ListingQueryPipeline(new PaginationValidator()));
}
