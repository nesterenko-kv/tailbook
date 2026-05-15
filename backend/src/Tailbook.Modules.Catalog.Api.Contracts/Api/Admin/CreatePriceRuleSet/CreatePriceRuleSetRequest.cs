namespace Tailbook.Modules.Catalog.Api.Admin.CreatePriceRuleSet;

public sealed class CreatePriceRuleSetRequest
{
    public DateTimeOffset? ValidFrom { get; set; }
    public DateTimeOffset? ValidTo { get; set; }
}