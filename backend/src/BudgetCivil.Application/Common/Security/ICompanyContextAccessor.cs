namespace BudgetCivil.Application.Common.Security;

public interface ICompanyContextAccessor
{
    Guid? CompanyId { get; }

    void SetCompanyId(Guid? companyId);
}
