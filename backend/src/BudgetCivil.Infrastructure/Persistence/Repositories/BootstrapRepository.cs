using System.Text.Json;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;
using BudgetCivil.Application.Users.Bootstrap.Dtos;
using BudgetCivil.Application.Users.Bootstrap.Repositories;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class BootstrapRepository(
    BudgetCivilDbContext dbContext,
    IPasswordHasher passwordHasher) : IBootstrapRepository
{
    private const string AdministratorRoleCode = "ADMINISTRADOR";

    public Task<bool> AnyAdministratorExistsAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(user => user.Role.Code == AdministratorRoleCode, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        return dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(user => user.Email.ToUpper() == normalizedEmail, cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim().ToUpperInvariant();
        return dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(user => user.Username.ToUpper() == normalizedUsername, cancellationToken);
    }

    public Task<bool> CompanyTaxIdExistsAsync(string taxId, CancellationToken cancellationToken = default)
    {
        var normalizedTaxId = taxId.Trim().ToUpperInvariant();
        return dbContext.Companies
            .AsNoTracking()
            .AnyAsync(company => company.TaxId.ToUpper() == normalizedTaxId, cancellationToken);
    }

    public async Task<(Guid AdministratorId, Guid AuditEventId)> CreateFirstAdministratorAsync(
        BootstrapRequest request,
        CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            if (dbContext.Database.IsRelational())
            {
                transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            }

            if (await AnyAdministratorExistsAsync(cancellationToken))
            {
                throw new ConflictAppException("Bootstrap is disabled because an administrator already exists.", "BOOTSTRAP_ALREADY_COMPLETED");
            }

            var administratorRole = await dbContext.Roles
                .SingleOrDefaultAsync(
                    role => role.Code == AdministratorRoleCode && role.IsActive,
                    cancellationToken);

            if (administratorRole is null)
            {
                throw new ConflictAppException("Administrator role baseline is unavailable.", "ADMIN_ROLE_NOT_AVAILABLE");
            }

            var utcNow = DateTime.UtcNow;

            var company = new Company
            {
                Id = Guid.NewGuid(),
                BusinessName = request.Company.BusinessName.Trim(),
                TaxId = request.Company.TaxId.Trim(),
                CompanyType = BudgetCivil.Domain.Companies.Company.NormalizeCompanyType(request.Company.CompanyType),
                TerritorialLevel = string.IsNullOrWhiteSpace(request.Company.TerritorialLevel)
                    ? null
                    : request.Company.TerritorialLevel.Trim(),
                CountryId = request.Company.CountryId,
                DepartmentId = request.Company.DepartmentId,
                MunicipalityId = request.Company.MunicipalityId,
                RepresentativeMode = request.Company.Representative!.Mode!.Trim().ToUpperInvariant(),
                RepresentativeUserId = request.Company.Representative.UserId,
                RepresentativeFullName = string.IsNullOrWhiteSpace(request.Company.Representative.FullName)
                    ? null
                    : request.Company.Representative.FullName.Trim(),
                RepresentativeDocumentTypeId = request.Company.Representative.DocumentTypeId,
                RepresentativeDocumentNumber = string.IsNullOrWhiteSpace(request.Company.Representative.DocumentNumber)
                    ? null
                    : request.Company.Representative.DocumentNumber.Trim(),
                IsActive = true,
                CreatedAtUtc = utcNow,
            };

            var administratorId = Guid.NewGuid();
            var administrator = new UserAccount
            {
                Id = administratorId,
                CompanyId = company.Id,
                RoleId = administratorRole.Id,
                Email = request.AdminProfile.Email.Trim(),
                Username = request.AdminProfile.Username.Trim(),
                PasswordHash = passwordHasher.HashPassword(request.AdminProfile.Password),
                AccountEnabled = true,
                AccountLocked = false,
                FailedLoginAttempts = 0,
                CreatedAtUtc = utcNow,
                UpdatedAtUtc = utcNow,
                FirstName = request.AdminProfile.FirstName.Trim(),
                LastName = request.AdminProfile.LastName.Trim(),
                DocumentType = request.AdminProfile.DocumentType.Trim(),
                DocumentNumber = request.AdminProfile.DocumentNumber.Trim(),
                BirthDate = new DateOnly(2000, 1, 1),
                CountryId = request.AdminProfile.CountryId,
                DepartmentId = request.AdminProfile.DepartmentId,
                MunicipalityId = request.AdminProfile.MunicipalityId
            };

            var auditEventId = Guid.NewGuid();
            var auditEvent = new SecurityAuditEvent
            {
                Id = auditEventId,
                ActorUserId = administratorId,
                CompanyId = company.Id,
                EventType = SecurityAuditEventType.Bootstrap,
                Result = SecurityAuditResult.Success,
                TargetType = nameof(UserAccount),
                TargetId = administratorId,
                ReasonCode = "FIRST_ADMINISTRATOR_CREATED",
                OccurredAtUtc = utcNow,
                MetadataJson = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["email"] = administrator.Email,
                    ["username"] = administrator.Username,
                    ["company_id"] = company.Id.ToString()
                })
            };

            dbContext.Companies.Add(company);
            dbContext.UserAccounts.Add(administrator);
            dbContext.SecurityAuditEvents.Add(auditEvent);

            await dbContext.SaveChangesAsync(cancellationToken);

            company.CreatedByUserId = administratorId;
            await dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return (administratorId, auditEventId);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<IReadOnlyList<BootstrapAuditRecordResponse>> GetBootstrapAuditAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SecurityAuditEvents
            .AsNoTracking()
            .Where(auditEvent => auditEvent.EventType == SecurityAuditEventType.Bootstrap)
            .OrderByDescending(auditEvent => auditEvent.OccurredAtUtc)
            .Select(auditEvent => new BootstrapAuditRecordResponse
            {
                Actor = auditEvent.ActorUser != null ? auditEvent.ActorUser.Username : "SYSTEM",
                Action = "BOOTSTRAP",
                Target = auditEvent.TargetType == null || auditEvent.TargetId == null
                    ? "N/A"
                    : $"{auditEvent.TargetType}:{auditEvent.TargetId}",
                OccurredAtUtc = auditEvent.OccurredAtUtc,
                Result = auditEvent.Result.ToString().ToUpperInvariant()
            })
            .ToListAsync(cancellationToken);
    }
}
