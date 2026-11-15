using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Application.Apus.Commands.CopyApu;
using BudgetCivil.Application.Apus.Commands.CopyMatrix;
using BudgetCivil.Application.Apus.Commands.MatrixComponents;
using BudgetCivil.Application.Apus.Dtos;
using BudgetCivil.Application.Apus.Queries;
using BudgetCivil.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

/// <summary>
/// Superficie v1 de APUs, Matrices, copia y conteo de uso (`D5`, `D-55`, `D-102`). Diez operaciones con
/// alcance de proyecto; el permiso se exige aquí y el alcance se valida en los casos de uso.
/// </summary>
[ApiController]
[Route("api/v1/projects/{projectId:guid}")]
public sealed class ApusController(
    GetApusQueryHandler getApusQueryHandler,
    GetApuByIdQueryHandler getApuByIdQueryHandler,
    GetMatrixComponentsQueryHandler getMatrixComponentsQueryHandler,
    GetResourceUsageQueryHandler getResourceUsageQueryHandler,
    CopyApuPreviewCommandHandler copyApuPreviewCommandHandler,
    CopyApuCommandHandler copyApuCommandHandler,
    AddMatrixComponentCommandHandler addMatrixComponentCommandHandler,
    UpdateMatrixComponentCommandHandler updateMatrixComponentCommandHandler,
    RemoveMatrixComponentCommandHandler removeMatrixComponentCommandHandler,
    CopyMatrixCommandHandler copyMatrixCommandHandler) : ControllerBase
{
    [HttpGet("apus")]
    [Authorize(Policy = AuthorizationPolicies.ApusRead)]
    [ProducesResponseType(typeof(ApuListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApuListResponse>> GetApus(
        [FromRoute] Guid projectId,
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? municipalityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var response = await getApusQueryHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            new GetApusQuery
            {
                Search = search,
                DepartmentId = departmentId,
                MunicipalityId = municipalityId,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Vista previa de la copia: no copia nada. Clasifica cada insumo simple como `EXACT` (misma
    /// descripción), `SIMILAR` (descripción parcial) o `NONE`, y para `EXACT`/`SIMILAR` devuelve hasta diez
    /// candidatos del proyecto destino con el comparativo de unidad y precio unitario
    /// (`comparison.unitMatches`, `comparison.unitPriceMatches`, `comparison.unitPriceDifference`).
    /// Las Matrices no se clasifican porque se copian siempre como registros nuevos.
    ///
    /// <para>
    /// Úsalo siempre antes de <c>POST /apus/copy</c>: los insumos con <c>requiresResolution = true</c> deben
    /// resolverse con <c>USE_EXISTING</c> (eligiendo un candidato) o <c>KEEP_ORIGIN</c>; los `NONE` se
    /// copian automáticamente. Si se omite una resolución, la copia responde
    /// <c>400 APU_COMPOSITION_RESOLUTION_REQUIRED</c>.
    /// </para>
    /// </summary>
    [HttpGet("apus/copy/preview")]
    [Authorize(Policy = AuthorizationPolicies.ApusWrite)]
    [ProducesResponseType(typeof(ApuCopyPreviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApuCopyPreviewResponse>> PreviewCopy(
        [FromRoute] Guid projectId,
        [FromQuery] Guid sourceApuId,
        CancellationToken cancellationToken)
    {
        var response = await copyApuPreviewCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            new CopyApuPreviewCommand
            {
                SourceApuId = sourceApuId
            },
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Confirma la copia de un APU de otro proyecto de la misma compañía y crea el ítem EDT del destino en
    /// la misma transacción. Se usa después del preview: envía una <c>resolution</c> por cada insumo
    /// <c>EXACT</c>/<c>SIMILAR</c> (<c>USE_EXISTING</c> + <c>existingResourceId</c>, o
    /// <c>KEEP_ORIGIN</c>); los <c>NONE</c> y las Matrices se copian sin resolución. El bloque
    /// <c>target</c> aporta <c>code</c>, <c>wbsChapterId</c> y <c>quantityWork</c>; un código duplicado
    /// responde <c>409 WBS_CODE_DUPLICATED</c>. El origen debe ser de la misma compañía
    /// (<c>404 APU_NOT_FOUND</c>) y el actor necesita <c>APUS_WRITE</c> con <c>ProjectMember</c> en el
    /// destino y lectura sobre el origen. Devuelve la respuesta plana del ítem+APU.
    /// </summary>
    [HttpPost("apus/copy")]
    [Authorize(Policy = AuthorizationPolicies.ApusWrite)]
    [ProducesResponseType(typeof(WbsItemApuResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<WbsItemApuResponse>> Copy(
        [FromRoute] Guid projectId,
        [FromBody] CopyApuCommand command,
        CancellationToken cancellationToken)
    {
        var response = await copyApuCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            command,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("apus/{apuId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ApusRead)]
    [ProducesResponseType(typeof(ApuDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApuDetailResponse>> GetApu(
        [FromRoute] Guid projectId,
        [FromRoute] Guid apuId,
        CancellationToken cancellationToken)
    {
        var response = await getApuByIdQueryHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            apuId,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("matrices/{matrixResourceId:guid}/components")]
    [Authorize(Policy = AuthorizationPolicies.ApusRead)]
    [ProducesResponseType(typeof(MatrixDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MatrixDetailResponse>> GetMatrixComponents(
        [FromRoute] Guid projectId,
        [FromRoute] Guid matrixResourceId,
        CancellationToken cancellationToken)
    {
        var response = await getMatrixComponentsQueryHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            matrixResourceId,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("matrices/{matrixResourceId:guid}/components")]
    [Authorize(Policy = AuthorizationPolicies.ApusWrite)]
    [ProducesResponseType(typeof(MatrixDetailResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<MatrixDetailResponse>> AddMatrixComponent(
        [FromRoute] Guid projectId,
        [FromRoute] Guid matrixResourceId,
        [FromBody] AddMatrixComponentCommand command,
        CancellationToken cancellationToken)
    {
        var response = await addMatrixComponentCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            matrixResourceId,
            command,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("matrices/{matrixResourceId:guid}/components/{componentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ApusWrite)]
    [ProducesResponseType(typeof(MatrixDetailResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MatrixDetailResponse>> UpdateMatrixComponent(
        [FromRoute] Guid projectId,
        [FromRoute] Guid matrixResourceId,
        [FromRoute] Guid componentId,
        [FromBody] UpdateMatrixComponentCommand command,
        CancellationToken cancellationToken)
    {
        var response = await updateMatrixComponentCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            matrixResourceId,
            componentId,
            command,
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("matrices/{matrixResourceId:guid}/components/{componentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ApusWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMatrixComponent(
        [FromRoute] Guid projectId,
        [FromRoute] Guid matrixResourceId,
        [FromRoute] Guid componentId,
        [FromQuery] string? rowVersion,
        CancellationToken cancellationToken)
    {
        await removeMatrixComponentCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            matrixResourceId,
            componentId,
            new RemoveMatrixComponentCommand { RowVersion = rowVersion },
            cancellationToken);

        return NoContent();
    }

    [HttpPost("matrices/copy")]
    [Authorize(Policy = AuthorizationPolicies.ApusWrite)]
    [ProducesResponseType(typeof(CopyMatrixResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CopyMatrixResponse>> CopyMatrix(
        [FromRoute] Guid projectId,
        [FromBody] CopyMatrixCommand command,
        CancellationToken cancellationToken)
    {
        var response = await copyMatrixCommandHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
            command,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("resource-usage")]
    [Authorize(Policy = AuthorizationPolicies.ResourcesRead)]
    [ProducesResponseType(typeof(ResourceUsageResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResourceUsageResponse>> GetResourceUsage(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var response = await getResourceUsageQueryHandler.ExecuteAsync(
            GetActorUserId(),
            projectId,
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
