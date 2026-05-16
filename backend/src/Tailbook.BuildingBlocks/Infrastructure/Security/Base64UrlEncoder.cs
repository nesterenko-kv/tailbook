using System.Buffers;
using System.Runtime.CompilerServices;

namespace Tailbook.BuildingBlocks.Infrastructure.Security;

public static class Base64UrlEncoder
{
    public static string Encode(ReadOnlySpan<byte> bytes)
    {
        var base64Length = ((bytes.Length + 2) / 3) * 4;
        var buffer = ArrayPool<char>.Shared.Rent(base64Length);

        try
        {
            Convert.TryToBase64Chars(bytes, buffer, out var charsWritten);
            var base64Span = buffer.AsSpan(0, charsWritten);

            var padCount = 0;
            if (charsWritten > 0 && base64Span[^1] == '=')
            {
                padCount = 1;
                if (charsWritten > 1 && base64Span[^2] == '=')
                {
                    padCount = 2;
                }
            }

            var resultLength = charsWritten - padCount;
            return string.Create(resultLength, base64Span[..resultLength], (dest, src) =>
            {
                for (var i = 0; i < src.Length; i++)
                {
                    dest[i] = src[i] switch
                    {
                        '+' => '-',
                        '/' => '_',
                        var c => c
                    };
                }
            });
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    public static byte[] Decode(string value)
    {
        var paddedLength = value.Length + ((4 - (value.Length % 4)) % 4);
        var buffer = ArrayPool<char>.Shared.Rent(paddedLength);

        try
        {
            var span = buffer.AsSpan(0, paddedLength);
            for (var i = 0; i < value.Length; i++)
            {
                span[i] = value[i] switch
                {
                    '-' => '+',
                    '_' => '/',
                    var c => c
                };
            }

            // Add padding
            for (var i = value.Length; i < paddedLength; i++)
            {
                span[i] = '=';
            }

            return Convert.FromBase64CharArray(buffer, 0, paddedLength);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }
}