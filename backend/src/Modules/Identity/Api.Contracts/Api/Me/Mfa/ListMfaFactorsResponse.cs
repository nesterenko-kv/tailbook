namespace Tailbook.Modules.Identity.Api.Me.Mfa;

public sealed class ListMfaFactorsResponse
{
    public IReadOnlyCollection<MfaFactorView> Items { get; set; } = [];
}
