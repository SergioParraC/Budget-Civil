namespace BudgetCivil.Application.Common.Security;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyHashedPassword(string hashedPassword, string providedPassword);

    bool NeedsRehash(string hashedPassword);
}
