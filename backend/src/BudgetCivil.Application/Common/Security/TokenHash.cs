using System.Security.Cryptography;
using System.Text;

namespace BudgetCivil.Application.Common.Security;

/// <summary>
/// Hash de los tokens de un solo uso que se envían por correo. El valor en claro nunca se persiste.
/// </summary>
public static class TokenHash
{
    public static string Compute(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }

    public static string CreateRawToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
