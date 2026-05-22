namespace Tailbook.Modules.Catalog.Api.Admin.AddOfferVersionComponent;

public sealed class AddOfferVersionComponentRequest
{
    public Guid VersionId { get; set; }
    public Guid ProcedureId { get; set; }
    public string ComponentRole { get; set; } = string.Empty;
    public int SequenceNo { get; set; }
    public bool DefaultExpected { get; set; } = true;
}