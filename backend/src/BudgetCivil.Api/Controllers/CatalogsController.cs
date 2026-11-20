using BudgetCivil.Api.Auth;
using BudgetCivil.Api.Middleware;
using BudgetCivil.Application.Catalogs.Commands.CreateUnit;
using BudgetCivil.Application.Catalogs.Dtos;
using BudgetCivil.Application.Catalogs.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/catalogs")]
[AllowWithoutCompanyContext]
public sealed class CatalogsController(
    GetCatalogItemsQueryHandler getCatalogItemsQueryHandler,
    GetUnitsQueryHandler getUnitsQueryHandler,
    CreateUnitCommandHandler createUnitCommandHandler) : ControllerBase
{
    // Anónimo: el autorregistro público selecciona la residencia sin sesión.
    [AllowAnonymous]
    [HttpGet("countries")]
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
                Page = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("departments")]
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
                Page = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("municipalities")]
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
                Page = pageNumber,
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
                Page = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpGet("document-types")]
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
                Page = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("units")]
    [Authorize(Policy = AuthorizationPolicies.CatalogsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<UnitDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UnitDto>>> GetUnits(CancellationToken cancellationToken)
    {
        var response = await getUnitsQueryHandler.ExecuteAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost("units")]
    [Authorize(Policy = AuthorizationPolicies.CatalogsWrite)]
    [ProducesResponseType(typeof(UnitDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UnitDto>> CreateUnit(
        [FromBody] CreateUnitCommand command,
        CancellationToken cancellationToken)
    {
        var response = await createUnitCommandHandler.ExecuteAsync(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
