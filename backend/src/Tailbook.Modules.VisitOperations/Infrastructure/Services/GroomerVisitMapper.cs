namespace Tailbook.Modules.VisitOperations.Infrastructure.Services;

internal static class GroomerVisitMapper
{
    public static GroomerVisitDetailView Map(VisitDetailView view)
    {
        var visit = new GroomerVisitPetView(
            view.Pet.Id,
            view.Pet.Name,
            view.Pet.AnimalTypeCode,
            view.Pet.AnimalTypeName,
            view.Pet.BreedName,
            view.Pet.CoatTypeCode,
            view.Pet.SizeCategoryCode);

        return new GroomerVisitDetailView(
            view.Id,
            view.AppointmentId,
            visit,
            view.Status,
            view.CheckedInAt,
            view.StartedAt,
            view.CompletedAt,
            view.ClosedAt,
            view.ServiceMinutes,
            view.ReservedMinutes,
            view.Items.Select(item => new GroomerVisitExecutionItemView(
                item.Id,
                item.AppointmentItemId,
                item.ItemType,
                item.OfferId,
                item.OfferVersionId,
                item.OfferCode,
                item.OfferDisplayName,
                item.Quantity,
                item.ServiceMinutes,
                item.ReservedMinutes,
                item.ExpectedComponents.Select(component => new GroomerVisitExpectedComponentView(
                    component.Id,
                    component.ProcedureId,
                    component.ProcedureCode,
                    component.ProcedureName,
                    component.ComponentRole,
                    component.SequenceNo,
                    component.DefaultExpected,
                    component.IsSkipped)).ToArray(),
                item.PerformedProcedures.Select(performed => new GroomerVisitPerformedProcedureView(
                    performed.Id,
                    performed.ProcedureId,
                    performed.ProcedureCode,
                    performed.ProcedureName,
                    performed.Status,
                    performed.Note,
                    performed.RecordedAt)).ToArray(),
                item.SkippedComponents.Select(skipped => new GroomerVisitSkippedComponentView(
                    skipped.Id,
                    skipped.OfferVersionComponentId,
                    skipped.ProcedureId,
                    skipped.ProcedureCode,
                    skipped.ProcedureName,
                    skipped.OmissionReasonCode,
                    skipped.Note,
                    skipped.RecordedAt)).ToArray())).ToArray(),
            view.CreatedAt,
            view.UpdatedAt);
    }
}
