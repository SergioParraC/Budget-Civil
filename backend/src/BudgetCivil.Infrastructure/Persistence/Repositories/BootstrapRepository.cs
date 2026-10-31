using System.Text.Json;
using BudgetCivil.Application.Users.Bootstrap.Dtos;
using BudgetCivil.Application.Users.Bootstrap.Repositories;
using BudgetCivil.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BudgetCivil.Infrastructure.Persistence.Repositories;

public sealed class BootstrapRepository(
    BudgetCivilDbContext dbContext,
    BudgetCivil.Application.Common.Security.IPasswordHasher passwordHasher) : IBootstrapRepository
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

    /// <summary>
    /// `SC-IDENTIDAD-69`: crea exactamente un Administrador **sin compañía** y registra el evento. No crea
    /// ninguna empresa; la empresa demo se crea después con `POST /companies`.
    /// </summary>
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
                throw new BudgetCivil.Application.Common.Exceptions.ConflictAppException(
                    "Bootstrap is disabled because an administrator already exists.",
                    "BOOTSTRAP_ALREADY_COMPLETED");
            }

            var administratorRole = await dbContext.Roles
                .SingleOrDefaultAsync(
                    role => role.Code == AdministratorRoleCode && role.IsActive,
                    cancellationToken);

            if (administratorRole is null)
            {
                throw new BudgetCivil.Application.Common.Exceptions.ConflictAppException(
                    "Administrator role baseline is unavailable.",
                    "ADMIN_ROLE_NOT_AVAILABLE");
            }

            var utcNow = DateTime.UtcNow;
            var administratorId = Guid.NewGuid();

            var administrator = new UserAccount
            {
                Id = administratorId,
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
                BirthDate = request.AdminProfile.BirthDate,
                CountryId = request.AdminProfile.CountryId,
                DepartmentId = request.AdminProfile.DepartmentId,
                MunicipalityId = request.AdminProfile.MunicipalityId
            };

            var auditEventId = Guid.NewGuid();
            var auditEvent = new SecurityAuditEvent
            {
                Id = auditEventId,
                ActorUserId = administratorId,
                CompanyId = null,
                EventType = SecurityAuditEventType.Bootstrap,
                Result = SecurityAuditResult.Success,
                TargetType = nameof(UserAccount),
                TargetId = administratorId,
                ReasonCode = "FIRST_ADMINISTRATOR_CREATED",
                OccurredAtUtc = utcNow,
                MetadataJson = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["email"] = administrator.Email,
                    ["username"] = administrator.Username
                })
            };

            dbContext.UserAccounts.Add(administrator);
            dbContext.SecurityAuditEvents.Add(auditEvent);

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
