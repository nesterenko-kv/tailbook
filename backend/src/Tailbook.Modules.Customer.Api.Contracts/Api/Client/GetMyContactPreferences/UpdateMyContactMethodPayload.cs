namespace Tailbook.Modules.Customer.Api.Client.GetMyContactPreferences;

public sealed class UpdateMyContactMethodPayload
{
    public string MethodType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string? Notes { get; set; }
}
