using System.Security.Cryptography;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private const string Prefix = "PBKDF2$";

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        if (!passwordHash.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var remaining = passwordHash.AsSpan(Prefix.Length);

        var firstDollar = remaining.IndexOf('$');
        if (firstDollar < 0 || firstDollar == 0)
        {
            return false;
        }

        if (!int.TryParse(remaining[..firstDollar], out var iterations))
        {
            return false;
        }

        remaining = remaining[(firstDollar + 1)..];
        var secondDollar = remaining.IndexOf('$');
        if (secondDollar < 0 || secondDollar == 0)
        {
            return false;
        }

        var saltBase64 = remaining[..secondDollar];
        var expectedBase64 = remaining[(secondDollar + 1)..];

        Span<byte> salt = stackalloc byte[SaltSize];
        Span<byte> expected = stackalloc byte[KeySize];
        Span<byte> actual = stackalloc byte[KeySize];

        if (!Convert.TryFromBase64Chars(saltBase64, salt, out var saltWritten) || saltWritten != SaltSize)
        {
            return false;
        }

        if (!Convert.TryFromBase64Chars(expectedBase64, expected, out var expectedWritten) || expectedWritten != KeySize)
        {
            return false;
        }

        Rfc2898DeriveBytes.Pbkdf2(password, salt, actual, iterations, HashAlgorithmName.SHA256);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
