using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public static class MfaRecoveryCodeHelpers
{
    internal const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string GenerateNormalizedCode(int length)
    {
        return string.Create(length, length, (buffer, len) =>
        {
            for (var i = 0; i < len; i++)
            {
                buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }
        });
    }

    public static string FormatRecoveryCode(string normalizedCode)
    {
        var groups = (normalizedCode.Length + 3) / 4;
        var resultLength = normalizedCode.Length + groups - 1;

        return string.Create(resultLength, normalizedCode, (buffer, state) =>
        {
            var src = state.AsSpan();
            var idx = 0;
            for (var i = 0; i < src.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                {
                    buffer[idx++] = '-';
                }

                buffer[idx++] = src[i];
            }
        });
    }

    public static string NormalizeRecoveryCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        var validCount = CountAlphaNumeric(code.AsSpan());
        if (validCount == 0)
        {
            return string.Empty;
        }

        return string.Create(validCount, code, (buffer, state) =>
        {
            var idx = 0;
            foreach (var c in state)
            {
                if (char.IsLetterOrDigit(c))
                {
                    buffer[idx++] = char.ToUpperInvariant(c);
                }
            }
        });
    }

    public static string GetCodeSuffix(string normalizedCode)
    {
        return normalizedCode.Length <= 4
            ? normalizedCode
            : normalizedCode[^4..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountAlphaNumeric(ReadOnlySpan<char> span)
    {
        var count = 0;
        for (var i = 0; i < span.Length; i++)
        {
            if (char.IsLetterOrDigit(span[i]))
            {
                count++;
            }
        }

        return count;
    }
}