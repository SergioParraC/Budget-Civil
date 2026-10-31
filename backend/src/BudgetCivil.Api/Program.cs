using System.Text;
using System.Threading.RateLimiting;
using BudgetCivil.Api.Auth;
using BudgetCivil.Api.Middleware;
using BudgetCivil.Application;
using BudgetCivil.Infrastructure;
using BudgetCivil.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
const string LocalDevelopmentCorsPolicy = "LocalDevelopmentCorsPolicy";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);

    // Autorización Bearer en Swagger UI: pega el accessToken de /auth/login sin el prefijo "Bearer".
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega únicamente el accessToken devuelto por POST /api/v1/auth/login (sin el prefijo 'Bearer')."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy(LocalDevelopmentCorsPolicy, policy =>
    {
        // El cliente Angular puede arrancar en cualquier puerto local (`ng serve` reasigna el
        // 4200 cuando esta ocupado), asi que el origen se valida por host y no por puerto.
        // `DnsSafeHost` normaliza IPv6 (`[::1]` -> `::1`).
        policy.SetIsOriginAllowed(origin =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            uri.DnsSafeHost is "localhost" or "127.0.0.1" or "::1")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var signingKey = jwtSection.GetValue<string>(nameof(JwtOptions.SigningKey))
    ?? throw new InvalidOperationException("JWT signing key is not configured.");
var issuer = jwtSection.GetValue<string>(nameof(JwtOptions.Issuer))
    ?? throw new InvalidOperationException("JWT issuer is not configured.");
var audience = jwtSection.GetValue<string>(nameof(JwtOptions.Audience))
    ?? throw new InvalidOperationException("JWT audience is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(
        AuthorizationPolicies.ApuPublicRead,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("APU_PUBLIC_READ"));

    options.AddPolicy(
        AuthorizationPolicies.BootstrapAuditRead,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("AUDIT_BOOTSTRAP_READ"));

    options.AddPolicy(
        AuthorizationPolicies.CatalogsRead,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("CATALOGS_READ"));

    options.AddPolicy(
        AuthorizationPolicies.UsersCreate,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("USERS_CREATE"));

    options.AddPolicy(
        AuthorizationPolicies.UsersUpdate,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("USERS_UPDATE"));

    options.AddPolicy(
        AuthorizationPolicies.UsersDeactivate,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("USERS_DEACTIVATE"));

    options.AddPolicy(
        AuthorizationPolicies.UsersRoleChange,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("USERS_ROLE_CHANGE"));

    options.AddPolicy(
        AuthorizationPolicies.UsersCompanyChange,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("USERS_COMPANY_CHANGE"));

    options.AddPolicy(
        AuthorizationPolicies.UsersRead,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("USERS_READ"));

    options.AddPolicy(
        AuthorizationPolicies.CompaniesCreate,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("COMPANIES_CREATE"));

    options.AddPolicy(
        AuthorizationPolicies.CompaniesUpdate,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("COMPANIES_UPDATE"));

    options.AddPolicy(
        AuthorizationPolicies.CompaniesDeactivate,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("COMPANIES_DEACTIVATE"));

    options.AddPolicy(
        AuthorizationPolicies.CompaniesReactivate,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("COMPANIES_REACTIVATE"));

    options.AddPolicy(
        AuthorizationPolicies.CompaniesRead,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("COMPANIES_READ"));

    options.AddPolicy(
        AuthorizationPolicies.CompanyProfileReadSelf,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("COMPANY_PROFILE_READ_SELF"));

    options.AddPolicy(
        AuthorizationPolicies.CompanyLeaveSelf,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("COMPANY_LEAVE_SELF"));

    options.AddPolicy(
        AuthorizationPolicies.CompanyMembersAssign,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("COMPANY_MEMBERS_ASSIGN"));

    options.AddPolicy(
        AuthorizationPolicies.CompanyMembersDeactivate,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("COMPANY_MEMBERS_DEACTIVATE"));

    options.AddPolicy(
        AuthorizationPolicies.ProjectsCreate,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("PROJECTS_CREATE"));

    options.AddPolicy(
        AuthorizationPolicies.ProjectsUpdate,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("PROJECTS_UPDATE"));

    options.AddPolicy(
        AuthorizationPolicies.ProjectsStatusChange,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("PROJECTS_STATUS_CHANGE"));

    options.AddPolicy(
        AuthorizationPolicies.ProjectsVisibilityChange,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("PROJECTS_VISIBILITY_CHANGE"));

    options.AddPolicy(
        AuthorizationPolicies.ProjectsRead,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("PROJECTS_READ"));

    options.AddPolicy(
        AuthorizationPolicies.ProjectMembersAssign,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("PROJECT_MEMBERS_ASSIGN"));

    options.AddPolicy(
        AuthorizationPolicies.ProjectMembersDeactivate,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("PROJECT_MEMBERS_DEACTIVATE"));

    options.AddPolicy(
        AuthorizationPolicies.ProfileReadSelf,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("PROFILE_READ_SELF"));

    options.AddPolicy(
        AuthorizationPolicies.ProfileUpdateSelf,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("PROFILE_UPDATE_SELF"));

    options.AddPolicy(
        AuthorizationPolicies.ProfileChangePasswordSelf,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("PROFILE_CHANGE_PASSWORD_SELF"));

    options.AddPolicy(
        AuthorizationPolicies.UsersPasswordRecoveryEnable,
        policy => policy
            .RequireAuthenticatedUser()
            .RequirePermission("USERS_PASSWORD_RECOVERY_ENABLE"));
});

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// REQ-IDENTIDAD-06 / D-80: límite de intentos de autorregistro por dirección IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        RateLimitPolicies.Registration,
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        var httpContext = context.HttpContext;
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Type = "https://httpstatuses.com/429",
                Detail = "Registration attempts limit exceeded for this IP address.",
                Instance = httpContext.Request.Path,
                Extensions =
                {
                    ["code"] = "REGISTRATION_RATE_LIMIT_EXCEEDED",
                    ["traceId"] = httpContext.TraceIdentifier
                }
            },
            cancellationToken);
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Mantiene el token entre recargas de la pagina de Swagger UI.
        options.ConfigObject.AdditionalItems["persistAuthorization"] = true;
    });
    app.UseCors(LocalDevelopmentCorsPolicy);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// En desarrollo el cliente Angular consume la API por HTTP: redirigir a HTTPS rompe la
// peticion (el navegador falla contra el certificado de desarrollo y `HttpClient` reporta
// `status: 0`). La redireccion se mantiene fuera de desarrollo.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<CompanyContextMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    // D-40: el esquema se aplica manualmente con el runbook (docs/runbook-migraciones.md).
    var securityBaselineSeeder = scope.ServiceProvider.GetRequiredService<ISecurityBaselineSeeder>();

    await securityBaselineSeeder.SeedAsync();
}

app.Run();

public partial class Program;
