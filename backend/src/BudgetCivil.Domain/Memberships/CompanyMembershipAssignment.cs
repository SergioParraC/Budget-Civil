using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Domain.Memberships;

/// <summary>
/// Reglas de alta de vínculos usuario-compañía sobre el agregado <see cref="UserAccount"/>.
/// Si el usuario ya tuvo un vínculo con la misma compañía se reactiva esa fila (conservando
/// <see cref="CompanyMembership.CreatedAtUtc"/>); si no, se registra una vinculación nueva.
/// </summary>
public static class CompanyMembershipAssignment
{
    public static CompanyMembership Attach(UserAccount user, Guid companyId, DateTime utcNow)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("Company id is required.", nameof(companyId));
        }

        var existing = user.Memberships.FirstOrDefault(membership => membership.CompanyId == companyId);
        if (existing is not null)
        {
            existing.Activate(utcNow);
            return existing;
        }

        var created = CompanyMembership.Create(user.Id, companyId, utcNow);
        user.Memberships.Add(created);
        return created;
    }
}
