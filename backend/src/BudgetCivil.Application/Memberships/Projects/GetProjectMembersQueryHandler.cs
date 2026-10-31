using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Memberships.Repositories;
using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Memberships.Projects;

public sealed class ProjectMemberItem
{
    public Guid MembershipId { get; init; }

    public Guid UserId { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string RoleCode { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;

    public bool AccountEnabled { get; init; }

    public DateTime AssignedAtUtc { get; init; }
}

public sealed class GetProjectMembersResponse
{
    public IReadOnlyList<ProjectMemberItem> Items { get; init; } = [];
}

/// <summary>
/// Miembros del proyecto (incluye presupuestadores, directores y auditores asignados). El alcance se
/// valida contra la compañía dueña del proyecto; el Administrador ve cualquiera.
/// </summary>
public sealed class GetProjectMembersQueryHandler(IProjectMembershipRepository repository)
{
    public async Task<GetProjectMembersResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var actor = await repository.GetActorByIdAsync(actorUserId, cancellationToken)
            ?? throw new ForbiddenAppException("Authenticated actor was not found.", "AUTH_USER_NOT_FOUND");

        if (!actor.AccountEnabled || actor.AccountLocked || !actor.Role.IsActive)
        {
            throw new ForbiddenAppException("Authenticated actor is not operational.", "ACTOR_NOT_ENABLED");
        }

        var project = await repository.GetProjectByIdAsync(projectId, cancellationToken)
            ?? throw new ConflictAppException("Project was not found.", "PROJECT_NOT_FOUND");

        if (actor.Role.Code != "ADMINISTRADOR" &&
            (!actor.CompanyId.HasValue || actor.CompanyId.Value != project.CompanyId))
        {
            throw new ForbiddenAppException("Actor cannot access project from another company.", "PROJECT_SCOPE_FORBIDDEN");
        }

        var rows = await repository.GetMembersAsync(projectId, cancellationToken);

        return new GetProjectMembersResponse
        {
            Items = rows.Select(row => new ProjectMemberItem
            {
                MembershipId = row.MembershipId,
                UserId = row.UserId,
                FirstName = row.FirstName,
                LastName = row.LastName,
                Username = row.Username,
                Email = row.Email,
                RoleCode = row.RoleCode,
                RoleName = row.RoleName,
                AccountEnabled = row.AccountEnabled,
                AssignedAtUtc = row.AssignedAtUtc
            }).ToArray()
        };
    }
}
