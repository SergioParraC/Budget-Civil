using System.Security.Claims;
using BudgetCivil.Api.Auth;
using BudgetCivil.Api.Middleware;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Listings.Queries;
using BudgetCivil.Application.Dtos;
using BudgetCivil.Application.UseCases;
using BudgetCivil.Application.Users.Administration.Dtos;
using BudgetCivil.Application.Users.Administration.Guards;
using BudgetCivil.Application.Users.Administration.UseCases;
using BudgetCivil.Application.Users.Search;
using BudgetCivil.Application.Users.Search.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetCivil.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(
    CreateUserUseCase createUserUseCase,
    UpdateUserUseCase updateUserUseCase,
    DeactivateUserUseCase deactivateUserUseCase,
    EnableUserUseCase enableUserUseCase,
    ChangeUserRoleUseCase changeUserRoleUseCase,
    ChangeUserCompanyUseCase changeUserCompanyUseCase,
    EnableUserPasswordRecoveryUseCase enableUserPasswordRecoveryUseCase,
    SearchUsersUseCase searchUsersUseCase,
    GetUserByIdUseCase getUserByIdUseCase,
    SearchRepresentativeCandidatesUseCase searchRepresentativeCandidatesUseCase,
    RejectProjectAssignmentGuard rejectProjectAssignmentGuard,
    GetScopedUsersQueryHandler getScopedUsersQueryHandler) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.UsersRead)]
    [AllowWithoutCompanyContext]
    [ProducesResponseType(typeof(IReadOnlyList<UserSearchResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSearchResult>>> SearchUsers(
        [FromQuery(Name = "email")] string? email,
        [FromQuery(Name = "username")] string? username,
        [FromQuery(Name = "nombre_apellidos")] string? fullName,
        [FromQuery(Name = "tipo_documento")] string? documentType,
        [FromQuery(Name = "numero_documento")] string? documentNumber,
        [FromQuery(Name = "search")] string? search,
        [FromQuery(Name = "roleCode")] string? roleCode,
        [FromQuery(Name = "unassignedOnly")] bool? unassignedOnly,
        [FromQuery(Name = "companyId")] Guid? companyId,
        [FromQuery(Name = "estado_cuenta")] bool? accountEnabled,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();

        var query = new UserSearchQuery
        {
            Email = email,
            Username = username,
            FullName = fullName,
            DocumentType = documentType,
            DocumentNumber = documentNumber,
            Search = search,
            RoleCode = roleCode,
            UnassignedOnly = unassignedOnly ?? false,
            CompanyId = companyId,
            AccountEnabled = accountEnabled
        };

        var response = await searchUsersUseCase.ExecuteAsync(actorUserId, query, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Candidatos a representante legal de una compañía (`D-87`): exige tipo y número de documento y
    /// admite nombre de usuario y correo como filtros opcionales.
    /// </summary>
    [HttpGet("representative-candidates")]
    [Authorize(Policy = AuthorizationPolicies.UsersRead)]
    [AllowWithoutCompanyContext]
    [ProducesResponseType(typeof(IReadOnlyList<UserSearchResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserSearchResult>>> SearchRepresentativeCandidates(
        [FromQuery(Name = "documentType")] string? documentType,
        [FromQuery(Name = "documentNumber")] string? documentNumber,
        [FromQuery(Name = "username")] string? username,
        [FromQuery(Name = "email")] string? email,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();

        var response = await searchRepresentativeCandidatesUseCase.ExecuteAsync(
            actorUserId,
            new UserSearchQuery
            {
                DocumentType = documentType,
                DocumentNumber = documentNumber,
                Username = username,
                Email = email
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{userId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.UsersRead)]
    [AllowWithoutCompanyContext]
    [ProducesResponseType(typeof(UserSearchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserSearchResult>> GetUserById(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await getUserByIdUseCase.ExecuteAsync(actorUserId, userId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("company/me")]
    [Authorize(Policy = AuthorizationPolicies.UsersRead)]
    [ProducesResponseType(typeof(GetScopedUsersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetScopedUsersResponse>> GetMyCompanyUsers(
        [FromQuery] string? search,
        [FromQuery] string? roleCode,
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = GetActorUserId();

        var response = await getScopedUsersQueryHandler.ExecuteAsync(
            actorUserId,
            new GetScopedUsersQuery
            {
                Scope = ScopedUsersListingScope.MyCompany,
                Search = search,
                RoleCode = roleCode,
                IsActive = isActive,
                Page = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("company/me/estimators-auditors")]
    [Authorize(Policy = AuthorizationPolicies.UsersRead)]
    [ProducesResponseType(typeof(GetScopedUsersResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetScopedUsersResponse>> GetMyCompanyEstimatorsAndAuditors(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = GetActorUserId();

        var response = await getScopedUsersQueryHandler.ExecuteAsync(
            actorUserId,
            new GetScopedUsersQuery
            {
                Scope = ScopedUsersListingScope.MyCompanyEstimatorsAuditors,
                Search = search,
                IsActive = isActive,
                Page = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.UsersCreate)]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<CreateUserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        rejectProjectAssignmentGuard.EnsureNoProjectAssignment(request.ExtensionData);

        var actorUserId = GetActorUserId();
        var response = await createUserUseCase.ExecuteAsync(actorUserId, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{userId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.UsersUpdate)]
    [ProducesResponseType(typeof(UpdateUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateUserResponse>> UpdateUser(
        [FromRoute] Guid userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        rejectProjectAssignmentGuard.EnsureNoProjectAssignment(request.ExtensionData);

        var actorUserId = GetActorUserId();
        var response = await updateUserUseCase.ExecuteAsync(actorUserId, userId, request, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{userId:guid}/deactivate")]
    [Authorize(Policy = AuthorizationPolicies.UsersDeactivate)]
    [ProducesResponseType(typeof(DeactivateUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeactivateUserResponse>> DeactivateUser(
        [FromRoute] Guid userId,
        [FromBody] DeactivateUserRequest request,
        CancellationToken cancellationToken)
    {
        rejectProjectAssignmentGuard.EnsureNoProjectAssignment(request.ExtensionData);

        var actorUserId = GetActorUserId();
        var response = await deactivateUserUseCase.ExecuteAsync(actorUserId, userId, request, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{userId:guid}/enable")]
    [Authorize(Policy = AuthorizationPolicies.UsersDeactivate)]
    [ProducesResponseType(typeof(EnableUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EnableUserResponse>> EnableUser(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await enableUserUseCase.ExecuteAsync(actorUserId, userId, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{userId:guid}/role")]
    [Authorize(Policy = AuthorizationPolicies.UsersRoleChange)]
    [ProducesResponseType(typeof(ChangeUserRoleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChangeUserRoleResponse>> ChangeUserRole(
        [FromRoute] Guid userId,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        rejectProjectAssignmentGuard.EnsureNoProjectAssignment(request.ExtensionData);

        var actorUserId = GetActorUserId();
        var response = await changeUserRoleUseCase.ExecuteAsync(actorUserId, userId, request, cancellationToken);

        return Ok(response);
    }

    [HttpPatch("{userId:guid}/company")]
    [Authorize(Policy = AuthorizationPolicies.UsersCompanyChange)]
    [ProducesResponseType(typeof(ChangeUserCompanyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChangeUserCompanyResponse>> ChangeUserCompany(
        [FromRoute] Guid userId,
        [FromBody] ChangeUserCompanyRequest request,
        CancellationToken cancellationToken)
    {
        rejectProjectAssignmentGuard.EnsureNoProjectAssignment(request.ExtensionData);

        var actorUserId = GetActorUserId();
        var response = await changeUserCompanyUseCase.ExecuteAsync(actorUserId, userId, request, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{userId:guid}/password-recovery/enable")]
    [Authorize(Policy = AuthorizationPolicies.UsersPasswordRecoveryEnable)]
    [ProducesResponseType(typeof(EnableUserPasswordRecoveryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EnableUserPasswordRecoveryResponse>> EnablePasswordRecovery(
        [FromRoute] Guid userId,
        [FromBody] EnableUserPasswordRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = GetActorUserId();
        var response = await enableUserPasswordRecoveryUseCase.ExecuteAsync(actorUserId, userId, request, cancellationToken);

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
