namespace BudgetCivil.Application.Projects.Queries;

public sealed class GetProjectConfigurationResponse
{
    public Guid ProjectId { get; init; }

    public Guid BaseCurrencyId { get; init; }

    public decimal? BudgetLimit { get; init; }

    public decimal? AiuAdminPercent { get; init; }

    public decimal? AiuUnforeseenPercent { get; init; }

    public decimal? AiuProfitPercent { get; init; }

    public string SignatureMode { get; init; } = string.Empty;

    public Guid? SignerUserId { get; init; }

    public bool RequiresAuditor { get; init; }
}

/// <summary>
/// Lectura de la configuración única del proyecto (`REQ-PROYECTOS-10`). Reutiliza las reglas de alcance
/// y visibilidad de <see cref="GetProjectByIdQueryHandler"/> para no duplicar la autorización.
/// </summary>
public sealed class GetProjectConfigurationQueryHandler(GetProjectByIdQueryHandler getProjectByIdQueryHandler)
{
    public async Task<GetProjectConfigurationResponse> ExecuteAsync(
        Guid actorUserId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var project = await getProjectByIdQueryHandler.ExecuteAsync(actorUserId, projectId, cancellationToken);

        return new GetProjectConfigurationResponse
        {
            ProjectId = project.ProjectId,
            BaseCurrencyId = project.BaseCurrencyId,
            BudgetLimit = project.BudgetLimit,
            AiuAdminPercent = project.AiuAdminPercent,
            AiuUnforeseenPercent = project.AiuUnforeseenPercent,
            AiuProfitPercent = project.AiuProfitPercent,
            SignatureMode = project.SignatureMode,
            SignerUserId = project.SignerUserId,
            RequiresAuditor = project.RequiresAuditor
        };
    }
}
