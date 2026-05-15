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

        return normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Take(MaxTerms)
            .ToArray();
    }

    public static string LikePattern(string term)
    {
        return $"%{EscapeLikePattern(term)}%";
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

        return terms.All(term => values.Any(value => value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true));
    }

    public static bool ContainsTerm(string term, params string?[] values)
    {
        return values.Any(value => value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }
}
