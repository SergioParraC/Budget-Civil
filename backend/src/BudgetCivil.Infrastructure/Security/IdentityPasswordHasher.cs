using BudgetCivil.Application.Common.Security;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Identity;

namespace BudgetCivil.Infrastructure.Security;

/// <summary>
/// D-81: el hash lo provee ASP.NET Core Identity (PBKDF2). DEC-04: un hash heredado se verifica y se
/// sustituye por rehash en el siguiente inicio de sesión correcto.
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private const string IdentityHashPrefix = "AQAAAA";
    private static readonly object IdentitySubject = new();
    private readonly PasswordHasher<object> passwordHasher = new();

    public string HashPassword(string password)
    {
        return passwordHasher.HashPassword(IdentitySubject, password);
    }

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            return false;
        }

        if (IsLegacyHash(hashedPassword))
        {
            return Argon2.Verify(hashedPassword, providedPassword);
        }

        return passwordHasher.VerifyHashedPassword(IdentitySubject, hashedPassword, providedPassword)
            != PasswordVerificationResult.Failed;
    }

    public bool NeedsRehash(string hashedPassword)
    {
        return string.IsNullOrWhiteSpace(hashedPassword) || IsLegacyHash(hashedPassword);
    }

    private static bool IsLegacyHash(string hashedPassword)
    {
        return !hashedPassword.StartsWith(IdentityHashPrefix, StringComparison.Ordinal);
    }
}