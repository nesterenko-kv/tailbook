using Tailbook.BuildingBlocks.Infrastructure.Search;
using Xunit;

namespace Tailbook.Modules.Pets.Tests;

public sealed class SearchTextTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("  ", null)]
    [InlineData("hello", "hello")]
    [InlineData("  hello  ", "hello")]
    [InlineData("hello world", "hello world")]
    public void Normalize_returns_null_for_whitespace_or_trimmed_value(string? input, string? expected)
    {
        var result = SearchText.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, new string[0])]
    [InlineData("", new string[0])]
    [InlineData("  ", new string[0])]
    [InlineData("hello", new[] { "hello" })]
    [InlineData("HELLO", new[] { "hello" })]
    [InlineData("hello world", new[] { "hello", "world" })]
    [InlineData("  hello   world  ", new[] { "hello", "world" })]
    [InlineData("hello hello", new[] { "hello" })]
    [InlineData("a b c d e f g", new[] { "a", "b", "c", "d", "e", "f" })]
    public void Terms_parses_and_deduplicates_with_max_six(string? input, string[] expected)
    {
        var result = SearchText.Terms(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("test", "%test%")]
    [InlineData("he%llo", "%he\\%llo%")]
    [InlineData("wor_ld", "%wor\\_ld%")]
    [InlineData("foo\\bar", "%foo\\\\bar%")]
    [InlineData("a%b_c\\d", "%a\\%b\\_c\\\\d%")]
    public void LikePattern_escapes_special_chars(string input, string expected)
    {
        var result = SearchText.LikePattern(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ContainsAllTerms_returns_true_when_terms_empty()
    {
        Assert.True(SearchText.ContainsAllTerms("anything", []));
    }

    [Fact]
    public void ContainsAllTerms_returns_false_when_value_null_or_whitespace()
    {
        Assert.False(SearchText.ContainsAllTerms(null, new[] { "term" }));
        Assert.False(SearchText.ContainsAllTerms("", new[] { "term" }));
        Assert.False(SearchText.ContainsAllTerms("  ", new[] { "term" }));
    }

    [Fact]
    public void ContainsAllTerms_matches_case_insensitive()
    {
        Assert.True(SearchText.ContainsAllTerms("Hello World", new[] { "hello", "WORLD" }));
    }

    [Fact]
    public void ContainsAllTerms_returns_false_when_any_term_missing()
    {
        Assert.False(SearchText.ContainsAllTerms("Hello World", new[] { "hello", "missing" }));
    }

    [Fact]
    public void ContainsAnyField_returns_true_when_terms_empty()
    {
        Assert.True(SearchText.ContainsAnyField([], "anything"));
    }

    [Fact]
    public void ContainsAnyField_matches_any_value()
    {
        Assert.True(SearchText.ContainsAnyField(new[] { "hello" }, "nope", "hello there", "nope2"));
    }

    [Fact]
    public void ContainsAnyField_requires_all_terms()
    {
        Assert.False(SearchText.ContainsAnyField(new[] { "hello", "world" }, "hello only"));
    }

    [Fact]
    public void ContainsTerm_matches_any_value()
    {
        Assert.True(SearchText.ContainsTerm("hello", (string?)null, "hello world"));
        Assert.False(SearchText.ContainsTerm("missing", "hello world", "goodbye"));
    }
}