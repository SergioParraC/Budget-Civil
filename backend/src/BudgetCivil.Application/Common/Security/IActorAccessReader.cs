namespace BudgetCivil.Application.Common.Security;

public interface IActorAccessReader
{
    /// <summary>
    /// Devuelve la foto del actor o <c>null</c> si el usuario no existe. <c>CompanyIsActive</c> es
    /// <c>null</c> cuando el actor no tiene compañía o cuando la compañía no existe.
    /// </summary>
    Task<ActorAccessSnapshot?> GetSnapshotAsync(Guid actorUserId, CancellationToken cancellationToken = default);
}
