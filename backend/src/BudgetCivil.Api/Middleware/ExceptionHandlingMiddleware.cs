using BudgetCivil.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BudgetCivil.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (AppException appException)
        {
            await WriteProblemDetailsAsync(httpContext, appException.StatusCode, appException.Message, appException.Code);
        }
        catch (SecurityTokenException securityTokenException)
        {
            await WriteProblemDetailsAsync(httpContext, 401, securityTokenException.Message, "INVALID_TOKEN");
        }
        catch (UnauthorizedAccessException unauthorizedAccessException)
        {
            await WriteProblemDetailsAsync(httpContext, 401, unauthorizedAccessException.Message, "UNAUTHORIZED");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception for request {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            await WriteProblemDetailsAsync(httpContext, 500, "An unexpected error occurred.", "UNHANDLED_ERROR");
        }
    }

    private static Task WriteProblemDetailsAsync(HttpContext httpContext, int statusCode, string detail, string? code)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Type = $"https://httpstatuses.com/{statusCode}",
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (!string.IsNullOrWhiteSpace(code))
        {
            problemDetails.Extensions["code"] = code;
        }

        httpContext.Response.StatusCode = statusCode;
        return httpContext.Response.WriteAsJsonAsync(problemDetails);
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            409 => "Conflict",
            423 => "Locked",
            _ => "Server Error"
        };
    }
}
