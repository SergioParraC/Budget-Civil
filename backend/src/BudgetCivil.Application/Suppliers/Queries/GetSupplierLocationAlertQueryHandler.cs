using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Resources.Repositories;
using BudgetCivil.Application.Suppliers.Dtos;
using BudgetCivil.Application.Suppliers.Repositories;
using BudgetCivil.Application.Suppliers.Services;

namespace BudgetCivil.Application.Suppliers.Queries;

public sealed class GetSupplierLocationAlertQuery
{
    public Guid ProjectId { get; init; }
}

/// <summary>
/// Alerta de ubicación no bloqueante (`REQ-PROVEEDORES-10`, `D-96`): compara el nivel más específico
/// que ambos tengan informado y nunca impide la asignación.
/// </summary>
public sealed class GetSupplierLocationAlertQueryHandler(
    SupplierAccessService supplierAccessService,
    ISupplierRepository supplierRepository,
    IResourceProjectAccessReader projectAccessReader)
{
    public async Task<LocationAlertDto> ExecuteAsync(
        Guid actorUserId,
        Guid supplierId,
        GetSupplierLocationAlertQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.ProjectId == Guid.Empty)
        {
            throw new ValidationAppException("projectId is required.", "VALIDATION_ERROR");
        }

        var scope = await supplierAccessService.EnsureReadAsync(actorUserId, null, cancellationToken);

        var supplier = await supplierRepository.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new NotFoundAppException("Supplier was not found.", "SUPPLIER_NOT_FOUND");

        SupplierAccessService.EnsureCompanyScope(supplier.CompanyId, scope.CompanyId);

        // Un proyecto de otra compañía responde como inexistente (`D-34`).
        var projectAccess = await projectAccessReader.GetAccessAsync(
            query.ProjectId,
            actorUserId,
            cancellationToken)
            ?? throw new NotFoundAppException("Project was not found.", "PROJECT_NOT_FOUND");

        if (projectAccess.CompanyId != supplier.CompanyId)
        {
            throw new NotFoundAppException("Project was not found.", "PROJECT_NOT_FOUND");
        }

        var projectLocation = await supplierRepository.GetProjectLocationAsync(
            query.ProjectId,
            cancellationToken)
            ?? throw new NotFoundAppException("Project was not found.", "PROJECT_NOT_FOUND");

        var supplierLocation = SupplierMapper.ToLocation(supplier);

        return Compare(supplier.Id, query.ProjectId, supplierLocation, projectLocation);
    }

    /// <summary>
    /// Comparación por nivel más específico disponible (`D6`): municipio, luego departamento y luego país.
    /// Un municipio distinto en el mismo departamento también es discrepancia.
    /// </summary>
    public static LocationAlertDto Compare(
        Guid supplierId,
        Guid projectId,
        LocationDto supplierLocation,
        SupplierLocation projectLocation)
    {
        var projectLocationDto = new LocationDto
        {
            CountryId = projectLocation.CountryId,
            CountryName = projectLocation.CountryName,
            DepartmentId = projectLocation.DepartmentId,
            DepartmentName = projectLocation.DepartmentName,
            MunicipalityId = projectLocation.MunicipalityId,
            MunicipalityName = projectLocation.MunicipalityName
        };

        if (supplierLocation.MunicipalityId.HasValue && projectLocation.MunicipalityId.HasValue)
        {
            return Build(
                supplierId,
                projectId,
                supplierLocation,
                projectLocationDto,
                supplierLocation.MunicipalityId == projectLocation.MunicipalityId);
        }

        if (supplierLocation.DepartmentId.HasValue && projectLocation.DepartmentId.HasValue)
        {
            return Build(
                supplierId,
                projectId,
                supplierLocation,
                projectLocationDto,
                supplierLocation.DepartmentId == projectLocation.DepartmentId);
        }

        if (supplierLocation.CountryId.HasValue && projectLocation.CountryId.HasValue)
        {
            return Build(
                supplierId,
                projectId,
                supplierLocation,
                projectLocationDto,
                supplierLocation.CountryId == projectLocation.CountryId);
        }

        return new LocationAlertDto
        {
            SupplierId = supplierId,
            ProjectId = projectId,
            CanCompare = false,
            IsSameLocation = false,
            Code = LocationAlertCodes.Incomplete,
            Message = "No hay ubicación suficiente en el proveedor o en el proyecto para comparar.",
            SupplierLocation = supplierLocation,
            ProjectLocation = projectLocationDto
        };
    }

    private static LocationAlertDto Build(
        Guid supplierId,
        Guid projectId,
        LocationDto supplierLocation,
        LocationDto projectLocation,
        bool isSameLocation)
    {
        return new LocationAlertDto
        {
            SupplierId = supplierId,
            ProjectId = projectId,
            CanCompare = true,
            IsSameLocation = isSameLocation,
            Code = isSameLocation ? LocationAlertCodes.Match : LocationAlertCodes.Mismatch,
            Message = isSameLocation
                ? "El proveedor y el proyecto están en la misma ubicación."
                : $"El proveedor está en {Describe(supplierLocation)} y el proyecto en {Describe(projectLocation)}.",
            SupplierLocation = supplierLocation,
            ProjectLocation = projectLocation
        };
    }

    private static string Describe(LocationDto location)
    {
        var parts = new[] { location.MunicipalityName, location.DepartmentName, location.CountryName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        return parts.Count == 0 ? "una ubicación sin datos" : string.Join(", ", parts);
    }
}
