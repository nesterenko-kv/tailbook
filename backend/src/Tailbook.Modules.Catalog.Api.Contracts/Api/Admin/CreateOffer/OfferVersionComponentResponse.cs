namespace Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

public sealed class OfferVersionComponentResponse
{
    public Guid Id { get; set; }
    public Guid OfferVersionId { get; set; }
    public Guid ProcedureId { get; set; }
    public string ProcedureCode { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public string ComponentRole { get; set; } = string.Empty;
    public int SequenceNo { get; set; }
    public bool DefaultExpected { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public static OfferVersionComponentResponse Map(OfferVersionComponentView view)
    {
        return new OfferVersionComponentResponse
        {
            Id = view.Id,
            OfferVersionId = view.OfferVersionId,
            ProcedureId = view.ProcedureId,
            ProcedureCode = view.ProcedureCode,
            ProcedureName = view.ProcedureName,
            ComponentRole = view.ComponentRole,
            SequenceNo = view.SequenceNo,
            DefaultExpected = view.DefaultExpected,
            CreatedAt = view.CreatedAt
        };
    }
}