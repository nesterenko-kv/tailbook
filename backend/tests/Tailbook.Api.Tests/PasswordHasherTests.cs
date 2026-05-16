using Tailbook.Modules.Identity.Infrastructure.Services;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_and_verify_roundtrip_succeeds()
    {
        var hash = _sut.Hash("my-password");

        Assert.True(_sut.Verify("my-password", hash));
    }

    [Fact]
    public void Verify_wrong_password_returns_false()
    {
        var hash = _sut.Hash("correct-password");

        Assert.False(_sut.Verify("wrong-password", hash));
    }

    [Theory]
    [InlineData(null, "somehash")]
    [InlineData("", "somehash")]
    [InlineData("password", null)]
    [InlineData("password", "")]
    [InlineData("password", "  ")]
    public void Verify_returns_false_for_invalid_inputs(string? password, string? hash)
    {
        Assert.False(_sut.Verify(password!, hash!));
    }

    [Fact]
    public void Verify_wrong_prefix_returns_false()
    {
        Assert.False(_sut.Verify("password", "INVALID$10000$salt$hash"));
    }

    [Fact]
    public void TryParseHashParts_parses_valid_format()
    {
        var remaining = "100000$saltvalue$keyvalue".AsSpan();

        var result = PasswordHasher.TryParseHashParts(remaining, out var iterations, out var saltBase64, out var expectedBase64);

        Assert.True(result);
        Assert.Equal(100000, iterations);
        Assert.Equal("saltvalue", saltBase64.ToString());
        Assert.Equal("keyvalue", expectedBase64.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("$")]
    [InlineData("100000$")] // missing key
    [InlineData("100000$salt")] // missing key
    [InlineData("notanumber$salt$key")]
    public void TryParseHashParts_returns_false_for_invalid_format(string remaining)
    {
        var result = PasswordHasher.TryParseHashParts(remaining.AsSpan(), out _, out _, out _);

        Assert.False(result);
    }
}