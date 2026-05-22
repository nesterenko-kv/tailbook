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

        Span<byte> salt = stackalloc byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        Span<byte> key = stackalloc byte[KeySize];
        Rfc2898DeriveBytes.Pbkdf2(password, salt, key, Iterations, HashAlgorithmName.SHA256);

        return FormatHash(salt, key);
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

        if (!TryParseHashParts(remaining, out var iterations, out var saltBase64, out var expectedBase64))
        {
            return false;
        }

        Span<byte> salt = stackalloc byte[SaltSize];
        if (!Convert.TryFromBase64Chars(saltBase64, salt, out var saltWritten) || saltWritten != SaltSize)
        {
            return false;
        }

        Span<byte> expected = stackalloc byte[KeySize];
        if (!Convert.TryFromBase64Chars(expectedBase64, expected, out var expectedWritten) || expectedWritten != KeySize)
        {
            return false;
        }

        Span<byte> actual = stackalloc byte[KeySize];
        Rfc2898DeriveBytes.Pbkdf2(password, salt, actual, iterations, HashAlgorithmName.SHA256);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string FormatHash(ReadOnlySpan<byte> salt, ReadOnlySpan<byte> key)
    {
        return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    internal static bool TryParseHashParts(
        ReadOnlySpan<char> remaining,
        out int iterations,
        out ReadOnlySpan<char> saltBase64,
        out ReadOnlySpan<char> expectedBase64)
    {
        iterations = 0;
        saltBase64 = default;
        expectedBase64 = default;

        var firstDollar = remaining.IndexOf('$');
        if (firstDollar <= 0)
        {
            return false;
        }

        if (!int.TryParse(remaining[..firstDollar], out iterations))
        {
            return false;
        }

        remaining = remaining[(firstDollar + 1)..];
        var secondDollar = remaining.IndexOf('$');
        if (secondDollar <= 0)
        {
            return false;
        }

        saltBase64 = remaining[..secondDollar];
        expectedBase64 = remaining[(secondDollar + 1)..];
        return true;
    }
}
