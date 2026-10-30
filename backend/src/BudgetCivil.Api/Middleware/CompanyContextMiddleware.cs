using System.Security.Claims;
using BudgetCivil.Application.Common.Exceptions;
using BudgetCivil.Application.Common.Security;

namespace BudgetCivil.Api.Middleware;

public sealed class CompanyContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        ICompanyContextAccessor companyContextAccessor,
        IActorAccessReader actorAccessReader)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            await next(httpContext);
            return;
        }

        var actorUserIdClaim = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");

        if (!Guid.TryParse(actorUserIdClaim, out var actorUserId))
        {
            throw new ForbiddenAppException("Authenticated user context is invalid.", "INVALID_AUTH_SUBJECT");
        }

        var identitySnapshot = await actorAccessReader.GetSnapshotAsync(actorUserId);
        if (identitySnapshot is null)
        {
            throw new ForbiddenAppException("Authenticated user does not exist.", "AUTH_USER_NOT_FOUND");
        }

        if (identitySnapshot.RoleCode != "ADMINISTRADOR" && identitySnapshot.CompanyId.HasValue)
        {
            if (identitySnapshot.CompanyIsActive is null)
            {
                throw new ForbiddenAppException("Company context does not exist.", "COMPANY_NOT_FOUND");
            }

            if (!identitySnapshot.CompanyIsActive.Value)
            {
                throw new ForbiddenAppException("Company is inactive.", "COMPANY_INACTIVE");
            }
        }

        var companyClaim = httpContext.User.FindFirstValue("company_id");

        if (!string.IsNullOrWhiteSpace(companyClaim) && !Guid.TryParse(companyClaim, out _))
        {
            throw new ForbiddenAppException("Company context claim has invalid format.", "INVALID_COMPANY_CLAIM");
        }

        var allowsMissingCompanyContext = httpContext.GetEndpoint()?
            .Metadata
            .GetMetadata<AllowWithoutCompanyContextAttribute>() is not null;

        if (identitySnapshot.RoleCode != "ADMINISTRADOR" &&
            identitySnapshot.CompanyId.HasValue &&
            string.IsNullOrWhiteSpace(companyClaim) &&
            !allowsMissingCompanyContext)
        {
            throw new ForbiddenAppException("Company context claim is required for this role.", "COMPANY_CLAIM_REQUIRED");
        }

        if (Guid.TryParse(companyClaim, out var claimedCompanyId))
        {
            if (!identitySnapshot.CompanyId.HasValue || claimedCompanyId != identitySnapshot.CompanyId.Value)
            {
                throw new ForbiddenAppException("Company context claim does not match user scope.", "COMPANY_CONTEXT_MISMATCH");
            }
        }

        if (identitySnapshot.RoleCode != "ADMINISTRADOR" &&
            !identitySnapshot.CompanyId.HasValue &&
            !allowsMissingCompanyContext)
        {
            throw new ForbiddenAppException("Company context is required for this role.", "COMPANY_CONTEXT_REQUIRED");
        }

        companyContextAccessor.SetCompanyId(identitySnapshot.CompanyId);
        httpContext.Items[nameof(ICompanyContextAccessor.CompanyId)] = identitySnapshot.CompanyId;

        await next(httpContext);
    }
}
