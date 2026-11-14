namespace BudgetCivil.Domain.Wbs;

/// <summary>
/// Actividad del EDT: una fila por proyecto y usuario con la fecha UTC de su última modificación
/// (`RF-14`, `D-08`, `D-120`). No guarda historial ni detalle de operaciones.
/// </summary>
public sealed class WbsProjectActivity
{
    private WbsProjectActivity()
    {
    }

    public Guid ProjectId { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime LastModifiedAtUtc { get; private set; }

    public static WbsProjectActivity Create(Guid projectId, Guid userId, DateTime utcNow)
    {
        if (projectId == Guid.Empty)
        {
            throw new InvalidOperationException("Project is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("WBS activity user is required.");
        }

        return new WbsProjectActivity
        {
            ProjectId = projectId,
            UserId = userId,
            LastModifiedAtUtc = utcNow
        };
    }

    /// <summary>Sobrescribe la última modificación del usuario; no crea historial (`SC-EDT-72`).</summary>
    public void Touch(DateTime utcNow)
    {
        LastModifiedAtUtc = utcNow;
    }
}
