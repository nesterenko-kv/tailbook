using Xunit;

namespace Tailbook.Modules.Identity.Tests;

public sealed class MfaRecoveryCodeHelpersTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(16)]
    public void GenerateNormalizedCode_returns_correct_length(int length)
    {
        var code = MfaRecoveryCodeHelpers.GenerateNormalizedCode(length);

        Assert.Equal(length, code.Length);
    }

    [Fact]
    public void GenerateNormalizedCode_uses_only_alphabet_chars()
    {
        var code = MfaRecoveryCodeHelpers.GenerateNormalizedCode(100);

        foreach (var c in code)
        {
            Assert.Contains(c, MfaRecoveryCodeHelpers.Alphabet);
        }
    }

    [Theory]
    [InlineData("ABCD1234", "ABCD-1234")]
    [InlineData("ABCDEFGH", "ABCD-EFGH")]
    [InlineData("ABCD", "ABCD")]
    [InlineData("AB", "AB")]
    [InlineData("ABCDEFGHIJKL", "ABCD-EFGH-IJKL")]
    public void FormatRecoveryCode_adds_dashes_every_four_chars(string input, string expected)
    {
        var result = MfaRecoveryCodeHelpers.FormatRecoveryCode(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("abcd", "ABCD")]
    [InlineData("a-b-c-d", "ABCD")]
    [InlineData("a1b2c3d4", "A1B2C3D4")]
    [InlineData("abc-def-ghi", "ABCDEFGHI")]
    [InlineData(" ABC def ", "ABCDEF")]
    public void NormalizeRecoveryCode_uppers_and_strips_non_alphanumeric(string? input, string expected)
    {
        var result = MfaRecoveryCodeHelpers.NormalizeRecoveryCode(input!);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ABCDEFGH", "EFGH")]
    [InlineData("ABCDEFGHIJ", "GHIJ")]
    [InlineData("ABCD", "ABCD")]
    [InlineData("AB", "AB")]
    [InlineData("A", "A")]
    public void GetCodeSuffix_returns_last_four_or_all(string input, string expected)
    {
        var result = MfaRecoveryCodeHelpers.GetCodeSuffix(input);

        Assert.Equal(expected, result);
    }
}