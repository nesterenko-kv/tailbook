namespace Tailbook.BuildingBlocks.Abstractions.Security;

public interface ISensitivePayloadProtector
{
    string Protect(string purpose, string plaintext);
    string Unprotect(string purpose, string protectedPayload);
}
