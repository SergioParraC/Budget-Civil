namespace BudgetCivil.Infrastructure.Persistence;

public static class DatabaseSchemas
{
    public const string Param = "param";
    public const string Iam = "iam";
    public const string Dbo = "dbo";
    public const string Audit = "audit";
    public const string Log = "log";

    public static readonly string[] All = [Param, Iam, Dbo, Audit, Log];
}
