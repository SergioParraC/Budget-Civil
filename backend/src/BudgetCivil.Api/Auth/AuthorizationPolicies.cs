namespace BudgetCivil.Api.Auth;

public static class AuthorizationPolicies
{
    public const string ApuPublicRead = "ApuPublicRead";
    public const string BootstrapAuditRead = "BootstrapAuditRead";
    public const string UsersCreate = "UsersCreate";
    public const string UsersUpdate = "UsersUpdate";
    public const string UsersDeactivate = "UsersDeactivate";
    public const string UsersRoleChange = "UsersRoleChange";
    public const string UsersCompanyChange = "UsersCompanyChange";
    public const string UsersRead = "UsersRead";
    public const string CompaniesCreate = "CompaniesCreate";
    public const string CompaniesUpdate = "CompaniesUpdate";
    public const string CompaniesDeactivate = "CompaniesDeactivate";
    public const string ProfileReadSelf = "ProfileReadSelf";
    public const string ProfileUpdateSelf = "ProfileUpdateSelf";
    public const string ProfileChangePasswordSelf = "ProfileChangePasswordSelf";
    public const string UsersPasswordRecoveryEnable = "UsersPasswordRecoveryEnable";
}
