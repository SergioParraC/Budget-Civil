using BudgetCivil.Application.Catalogs.Repositories;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Domain.Catalogs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class MeasurementUnitRepository(BudgetCivilDbContext dbContext) : IMeasurementUnitRepository
{
    public async Task<IReadOnlyList<MeasurementUnit>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.MeasurementUnits
            .AsNoTracking()
            .OrderBy(unit => unit.ShortUnit)
            .ThenBy(unit => unit.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ShortUnitExistsAsync(
        string shortUnit,
        CancellationToken cancellationToken = default)
    {
        var normalized = shortUnit.Trim().ToUpperInvariant();

        return dbContext.MeasurementUnits.AnyAsync(
            unit => unit.ShortUnit.ToUpper() == normalized,
            cancellationToken);
    }

    public Task AddAsync(MeasurementUnit unit, CancellationToken cancellationToken = default)
    {
        dbContext.MeasurementUnits.Add(unit);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Una violación concurrente de `UQ_dbo_measurement_units_short_unit` se traduce al mismo
            // error de negocio que la verificación previa (`REQ-UNIDADES-02`, `SC-UNIDADES-09`).
            throw new ConflictAppException(
                "Measurement unit already exists.",
                "UNIT_ALREADY_EXISTS");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
            sqlException.Number is 2601 or 2627;
    }
}