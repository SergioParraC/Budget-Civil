using BudgetCivil.Domain.Users.Entities;

namespace BudgetCivil.Application.Common.Security;

public interface IJwtTokenService
{
    AccessTokenResult GenerateToken(UserAccount userAccount, IEnumerable<string> permissionKeys);
}
