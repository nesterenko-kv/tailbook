using System.Text;
using Tailbook.BuildingBlocks.Infrastructure.Security;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class Base64UrlEncoderTests
{
    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("Hello, World!")]
    [InlineData("a")]
    [InlineData("ab")]
    [InlineData("abc")]
    public void Encode_Decode_roundtrip(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var encoded = Base64UrlEncoder.Encode(bytes);
        var decoded = Base64UrlEncoder.Decode(encoded);

        var result = Encoding.UTF8.GetString(decoded);
        Assert.Equal(input, result);
    }

    [Fact]
    public void Encode_contains_no_padding_or_base64_chars()
    {
        var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        var encoded = Base64UrlEncoder.Encode(bytes);

        Assert.DoesNotContain('=', encoded);
        Assert.DoesNotContain('+', encoded);
        Assert.DoesNotContain('/', encoded);
    }

    [Fact]
    public void Encode_uses_url_safe_chars()
    {
        // bytes that encode to include + and / in standard base64
        var bytes = new byte[] { 0xFB, 0xFF, 0xFF, 0xFA };
        var encoded = Base64UrlEncoder.Encode(bytes);

        Assert.DoesNotContain('+', encoded);
        Assert.DoesNotContain('/', encoded);
    }

    [Theory]
    [InlineData("AA")]
    [InlineData("AA-")]
    [InlineData("AA_A")]
    public void Decode_handles_url_safe_format(string encoded)
    {
        var decoded = Base64UrlEncoder.Decode(encoded);

        Assert.NotNull(decoded);
        Assert.NotEmpty(decoded);
    }
}