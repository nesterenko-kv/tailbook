using System.Buffers;
using System.Runtime.CompilerServices;

namespace Tailbook.BuildingBlocks.Infrastructure.Search;

public static class SearchText
{
    private const int MaxTerms = 6;

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    public static string[] Terms(string? value)
    {
        var normalized = Normalize(value);
        if (normalized is null)
        {
            return [];
        }

        var span = normalized.AsSpan();
        var seen = new HashSet<string>(MaxTerms, StringComparer.Ordinal);
        var pooledList = ArrayPool<string>.Shared.Rent(MaxTerms);
        var count = 0;

        try
        {
            while (seen.Count < MaxTerms)
            {
                span = TrimStart(span);
                if (span.Length == 0) break;

                var spaceIdx = span.IndexOf(' ');
                ReadOnlySpan<char> term;
                if (spaceIdx < 0)
                {
                    term = TrimEnd(span);
                    span = [];
                }
                else
                {
                    term = TrimEnd(span[..spaceIdx]);
                    span = span[(spaceIdx + 1)..];
                }

                if (term.Length == 0) continue;

                var lowerTerm = ToLowerInvariant(term);
                if (seen.Add(lowerTerm))
                {
                    pooledList[count++] = lowerTerm;
                }
            }

            var result = new string[count];
            Array.Copy(pooledList, result, count);
            return result;
        }
        finally
        {
            ArrayPool<string>.Shared.Return(pooledList);
        }
    }

    public static string LikePattern(string term)
    {
        var escaped = EscapeLikePattern(term.AsSpan());
        return string.Create(escaped.Length + 2, escaped, (buffer, state) =>
        {
            buffer[0] = '%';
            state.CopyTo(buffer[1..]);
            buffer[^1] = '%';
        });
    }

    public static bool ContainsAllTerms(string? value, IReadOnlyCollection<string> terms)
    {
        if (terms.Count == 0)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return terms.All(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ContainsAnyField(IReadOnlyCollection<string> terms, params string?[] values)
    {
        if (terms.Count == 0)
        {
            return true;
        }

        return terms.All(term => ContainsInValues(term.AsSpan(), values));
    }

    public static bool ContainsTerm(string term, params string?[] values)
    {
        return ContainsInValues(term.AsSpan(), values);
    }

    private static bool ContainsInValues(ReadOnlySpan<char> term, string?[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (value is not null && value.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string EscapeLikePattern(ReadOnlySpan<char> value)
    {
        var totalExtra = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == '\\' || c == '%' || c == '_')
            {
                totalExtra++;
            }
        }

        if (totalExtra == 0)
        {
            return value.ToString();
        }

        return string.Create(value.Length + totalExtra, value, (buffer, state) =>
        {
            var src = state;
            var idx = 0;
            for (var i = 0; i < src.Length; i++)
            {
                var c = src[i];
                if (c == '\\' || c == '%' || c == '_')
                {
                    buffer[idx++] = '\\';
                }

                buffer[idx++] = c;
            }
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> TrimStart(ReadOnlySpan<char> span)
    {
        var start = 0;
        while (start < span.Length && span[start] == ' ') start++;
        return span[start..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<char> TrimEnd(ReadOnlySpan<char> span)
    {
        var end = span.Length - 1;
        while (end >= 0 && span[end] == ' ') end--;
        return span[..(end + 1)];
    }

    private static string ToLowerInvariant(ReadOnlySpan<char> span)
    {
        return string.Create(span.Length, span, (buffer, state) =>
        {
            for (var i = 0; i < state.Length; i++)
            {
                buffer[i] = char.ToLowerInvariant(state[i]);
            }
        });
    }
}
