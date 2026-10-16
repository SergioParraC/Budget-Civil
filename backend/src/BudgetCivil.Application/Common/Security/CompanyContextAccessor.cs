namespace BudgetCivil.Application.Common.Security;

public sealed class CompanyContextAccessor : ICompanyContextAccessor
{
    public Guid? CompanyId { get; private set; }

    public void SetCompanyId(Guid? companyId)
    {
        CompanyId = companyId;
    }
}
