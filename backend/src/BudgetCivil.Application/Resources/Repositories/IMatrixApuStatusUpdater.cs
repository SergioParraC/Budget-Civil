namespace BudgetCivil.Application.Resources.Repositories;

/// <summary>
/// Recálculo del estado de los APUs que usan una Matriz cuando cambia su unidad (`REQ-APUS-03`, `D4`,
/// `SC-APUS-100`). `M-04` lo declara y `Infrastructure` lo implementa sobre el mismo `DbContext` para que
/// el cambio y el recálculo ocurran en la misma transacción.
/// </summary>
public interface IMatrixApuStatusUpdater
{
    /// <summary>
    /// Recalcula el estado de cotización de la Matriz (unidad y componentes) y el estado de los APUs del
    /// proyecto que la usan (`REQ-APUS-03`, `REQ-APUS-06`, `D4`).
    /// </summary>
    Task RecalculateMatrixAsync(
        Guid projectId,
        Guid matrixResourceId,
        CancellationToken cancellationToken = default);

    Task RecalculateForMatrixAsync(
        Guid projectId,
        Guid matrixResourceId,
        CancellationToken cancellationToken = default);
}
