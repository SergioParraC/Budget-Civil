using BudgetCivil.Api.Auth;
using BudgetCivil.Application.Catalogs.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/catalogs")]
public sealed class CatalogsController(GetCatalogItemsQueryHandler getCatalogItemsQueryHandler) : ControllerBase
{
    [HttpGet("countries")]
    [Authorize(Policy = AuthorizationPolicies.CatalogsRead)]
    [ProducesResponseType(typeof(CatalogListingResponse<CountryCatalogItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogListingResponse<CountryCatalogItem>>> GetCountries(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await getCatalogItemsQueryHandler.GetCountriesAsync(
            new CatalogListingQuery
            {
                Search = search,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("departments")]
    [Authorize(Policy = AuthorizationPolicies.CatalogsRead)]
    [ProducesResponseType(typeof(CatalogListingResponse<DepartmentCatalogItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogListingResponse<DepartmentCatalogItem>>> GetDepartments(
        [FromQuery] Guid? countryId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await getCatalogItemsQueryHandler.GetDepartmentsAsync(
            new DepartmentCatalogListingQuery
            {
                CountryId = countryId,
                Search = search,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("municipalities")]
    [Authorize(Policy = AuthorizationPolicies.CatalogsRead)]
    [ProducesResponseType(typeof(CatalogListingResponse<MunicipalityCatalogItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogListingResponse<MunicipalityCatalogItem>>> GetMunicipalities(
        [FromQuery] Guid? departmentId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await getCatalogItemsQueryHandler.GetMunicipalitiesAsync(
            new MunicipalityCatalogListingQuery
            {
                DepartmentId = departmentId,
                Search = search,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("currencies")]
    [Authorize(Policy = AuthorizationPolicies.CatalogsRead)]
    [ProducesResponseType(typeof(CatalogListingResponse<CurrencyCatalogItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogListingResponse<CurrencyCatalogItem>>> GetCurrencies(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await getCatalogItemsQueryHandler.GetCurrenciesAsync(
            new CatalogListingQuery
            {
                Search = search,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("document-types")]
    [Authorize(Policy = AuthorizationPolicies.CatalogsRead)]
    [ProducesResponseType(typeof(CatalogListingResponse<DocumentTypeCatalogItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogListingResponse<DocumentTypeCatalogItem>>> GetDocumentTypes(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await getCatalogItemsQueryHandler.GetDocumentTypesAsync(
            new CatalogListingQuery
            {
                Search = search,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }
}
