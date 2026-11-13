using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Suppliers.Commands.ChangeSupplierStatus;
using BudgetCivil.Application.Suppliers.Commands.CreateSupplier;
using BudgetCivil.Application.Suppliers.Commands.UpdateSupplier;
using BudgetCivil.Application.Suppliers.Dtos;
using BudgetCivil.Application.Suppliers.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

/// <summary>
/// Directorio de proveedores de la compañía (`M-04`, `REQ-PROVEEDORES-*`). El alcance se resuelve con
/// el actor; nunca se acepta `companyId` en el cuerpo (`D-34`).
/// </summary>
[ApiController]
[Route("api/v1/suppliers")]
public sealed class SuppliersController(
    GetSuppliersQueryHandler getSuppliersQueryHandler,
    GetSupplierByIdQueryHandler getSupplierByIdQueryHandler,
    GetSupplierOptionsQueryHandler getSupplierOptionsQueryHandler,
    GetSupplierLocationAlertQueryHandler getSupplierLocationAlertQueryHandler,
    CreateSupplierCommandHandler createSupplierCommandHandler,
    UpdateSupplierCommandHandler updateSupplierCommandHandler,
    ChangeSupplierStatusCommandHandler changeSupplierStatusCommandHandler) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.SuppliersRead)]
    [ProducesResponseType(typeof(GetSuppliersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetSuppliersResponse>> GetSuppliers(
        [FromQuery] string? businessName,
        [FromQuery] string? contactName,
        [FromQuery] Guid? countryId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? municipalityId,
        [FromQuery] Guid? companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await getSuppliersQueryHandler.ExecuteAsync(
            GetActorUserId(),
            companyId,
            new GetSuppliersQuery
            {
                BusinessName = businessName,
                ContactName = contactName,
                CountryId = countryId,
                DepartmentId = departmentId,
                MunicipalityId = municipalityId,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("options")]
    [Authorize(Policy = AuthorizationPolicies.SuppliersRead)]
    [ProducesResponseType(typeof(GetSupplierOptionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetSupplierOptionsResponse>> GetSupplierOptions(
        [FromQuery] string? businessName,
        [FromQuery] string? contactName,
        [FromQuery] Guid? countryId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? municipalityId,
        [FromQuery] Guid? companyId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await getSupplierOptionsQueryHandler.ExecuteAsync(
            GetActorUserId(),
            companyId,
            new GetSupplierOptionsQuery
            {
                BusinessName = businessName,
                ContactName = contactName,
                CountryId = countryId,
                DepartmentId = departmentId,
                MunicipalityId = municipalityId,
                Limit = limit
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{supplierId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuppliersRead)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierDto>> GetSupplierById(
        [FromRoute] Guid supplierId,
        CancellationToken cancellationToken)
    {
        var response = await getSupplierByIdQueryHandler.ExecuteAsync(
            GetActorUserId(),
            supplierId,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{supplierId:guid}/location-alert")]
    [Authorize(Policy = AuthorizationPolicies.SuppliersRead)]
    [ProducesResponseType(typeof(LocationAlertDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LocationAlertDto>> GetLocationAlert(
        [FromRoute] Guid supplierId,
        [FromQuery] Guid projectId,
        CancellationToken cancellationToken)
    {
        var response = await getSupplierLocationAlertQueryHandler.ExecuteAsync(
            GetActorUserId(),
            supplierId,
            new GetSupplierLocationAlertQuery { ProjectId = projectId },
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SuppliersWrite)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<SupplierDto>> CreateSupplier(
        [FromBody] CreateSupplierCommand command,
        CancellationToken cancellationToken)
    {
        var response = await createSupplierCommandHandler.ExecuteAsync(
            GetActorUserId(),
            command,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{supplierId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SuppliersWrite)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierDto>> UpdateSupplier(
        [FromRoute] Guid supplierId,
        [FromBody] UpdateSupplierCommand command,
        CancellationToken cancellationToken)
    {
        var response = await updateSupplierCommandHandler.ExecuteAsync(
            GetActorUserId(),
            supplierId,
            command,
            cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{supplierId:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.SuppliersWrite)]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupplierDto>> ChangeSupplierStatus(
        [FromRoute] Guid supplierId,
        [FromBody] ChangeSupplierStatusCommand command,
        CancellationToken cancellationToken)
    {
        var response = await changeSupplierStatusCommandHandler.ExecuteAsync(
            GetActorUserId(),
            supplierId,
            command,
            cancellationToken);

        return Ok(response);
    }

    private Guid GetActorUserId()
    {
        var actorUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(actorUserIdClaim, out var actorUserId))
        {
            throw new ForbiddenAppException("Authenticated user context is invalid.", "INVALID_AUTH_SUBJECT");
        }

        return actorUserId;
    }
}
