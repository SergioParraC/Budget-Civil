using BudgetCivil.Application.Common.Security;
using Isopoh.Cryptography.Argon2;

namespace BudgetCivil.Infrastructure.Security;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return Argon2.Hash(password);
    }

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        return Argon2.Verify(hashedPassword, providedPassword);
    }
}
