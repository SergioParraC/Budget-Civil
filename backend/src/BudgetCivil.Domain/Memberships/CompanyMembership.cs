namespace BudgetCivil.Domain.Memberships;

/// <summary>
/// Vínculo entre un usuario y una compañía (tabla <c>users_company</c>). Es la única fuente de verdad
/// de la pertenencia: <see cref="AccountEnabled"/> indica si el vínculo está vigente y las filas se
/// conservan para mantener el historial de compañías por las que pasó el usuario.
/// </summary>
public sealed class CompanyMembership
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CompanyId { get; set; }

    /// <summary>
    /// Estado del vínculo (columna <c>account_enable</c>). Es independiente del estado de la cuenta
    /// (<c>users.account_enabled</c>): al salir de la compañía la cuenta sigue habilitada.
    /// </summary>
    public bool AccountEnabled { get; set; } = true;

    /// <summary>Fecha de la primera vinculación con la compañía.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha de la última modificación del vínculo.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public static CompanyMembership Create(Guid userId, Guid companyId, DateTime utcNow)
    {
        // La clave la genera la persistencia (EF/`uniqueidentifier`): asignarla aquí haría que EF
        // interpretara el vínculo nuevo como una fila existente y emitiera un UPDATE.
        return new CompanyMembership
        {
            UserId = userId,
            CompanyId = companyId,
            AccountEnabled = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    /// <summary>
    /// Reactiva un vínculo histórico con la misma compañía sin perder la fecha de vinculación original.
    /// </summary>
    public void Activate(DateTime utcNow)
    {
        AccountEnabled = true;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Cierra el vínculo conservando la fila para el historial del usuario.
    /// </summary>
    public void Deactivate(DateTime utcNow)
    {
        AccountEnabled = false;
        UpdatedAtUtc = utcNow;
    }

    public void Touch(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
    }
}
