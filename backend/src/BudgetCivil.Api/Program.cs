using System.Text;
using BudgetCivil.Api.Auth;
using BudgetCivil.Api.Middleware;
using BudgetCivil.Application;
using BudgetCivil.Infrastructure;
using BudgetCivil.Infrastructure.Persistence;
using BudgetCivil.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<CompanyContextMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BudgetCivilDbContext>();
    var securityBaselineSeeder = scope.ServiceProvider.GetRequiredService<ISecurityBaselineSeeder>();

    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }

    await securityBaselineSeeder.SeedAsync();
}

app.Run();

public partial class Program;
