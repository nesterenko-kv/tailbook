using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions.Security;

namespace Tailbook.BuildingBlocks.Infrastructure.Security;

public sealed class AesGcmSensitivePayloadProtector : ISensitivePayloadProtector
{
    private const string Version = "v1";
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private readonly byte[] _key;
    private readonly byte[]? _previousKey;

    public AesGcmSensitivePayloadProtector(IOptions<SensitivePayloadProtectionOptions> options)
    {
        _key = DeriveKey(options.Value.Key);
        _previousKey = string.IsNullOrWhiteSpace(options.Value.PreviousKey) ? null : DeriveKey(options.Value.PreviousKey);
    }

    public string Protect(string purpose, string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentNullException.ThrowIfNull(plaintext);

        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];
        var associatedData = Encoding.UTF8.GetBytes(purpose);

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, associatedData);

        return string.Join('.', Version, Base64UrlEncode(nonce), Base64UrlEncode(ciphertext), Base64UrlEncode(tag));
    }

    public string Unprotect(string purpose, string protectedPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedPayload);

        var parts = protectedPayload.Split('.', 4);
        if (parts.Length != 4 || !string.Equals(parts[0], Version, StringComparison.Ordinal))
        {
            throw new FormatException("Unsupported protected payload format.");
        }

        var nonce = Base64UrlDecode(parts[1]);
        var ciphertext = Base64UrlDecode(parts[2]);
        var tag = Base64UrlDecode(parts[3]);
        var associatedData = Encoding.UTF8.GetBytes(purpose);

        foreach (var candidateKey in GetCandidateKeys())
        {
            try
            {
                var plaintextBytes = new byte[ciphertext.Length];
                using var aes = new AesGcm(candidateKey, TagSizeBytes);
                aes.Decrypt(nonce, ciphertext, tag, plaintextBytes, associatedData);
                return Encoding.UTF8.GetString(plaintextBytes);
            }
            catch (CryptographicException)
            {
            }
        }

        throw new InvalidOperationException("Protected payload could not be unprotected with any configured key.");
    }

    private IEnumerable<byte[]> GetCandidateKeys()
    {
        yield return _key;
        if (_previousKey is not null)
        {
            yield return _previousKey;
        }
    }

    private static byte[] DeriveKey(string key)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(key));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            0 => string.Empty,
            2 => "==",
            3 => "=",
            _ => throw new FormatException("Invalid base64url payload.")
        };

        return Convert.FromBase64String(padded);
    }
}
