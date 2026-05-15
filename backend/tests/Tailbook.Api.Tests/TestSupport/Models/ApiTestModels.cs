namespace Tailbook.Api.Tests.TestSupport.Models;

internal sealed record PetCatalogSelection(
    string DogAnimalTypeCode,
    Guid DogAnimalTypeId,
    string DoubleCoatCode,
    string LargeSizeCode,
    Guid SamoyedBreedId);

internal sealed class PetCatalogEnvelope
{
    public CatalogAnimalTypeEnvelope[] AnimalTypes { get; set; } = [];
    public CatalogBreedEnvelope[] Breeds { get; set; } = [];
    public CatalogCoatTypeEnvelope[] CoatTypes { get; set; } = [];
    public CatalogSizeCategoryEnvelope[] SizeCategories { get; set; } = [];

    public PetCatalogSelection SelectSamoyed()
    {
        var dog = AnimalTypes.Single(x => x.Code == "DOG");

        return new PetCatalogSelection(
            dog.Code,
            dog.Id,
            CoatTypes.Single(x => x.Code == "DOUBLE_COAT").Code,
            SizeCategories.Single(x => x.Code == "LARGE").Code,
            Breeds.Single(x => x.Code == "SAMOYED").Id);
    }
}

internal sealed class CatalogAnimalTypeEnvelope
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

internal sealed class CatalogBreedEnvelope
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

internal sealed class CatalogCoatTypeEnvelope
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

internal sealed class CatalogSizeCategoryEnvelope
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

internal sealed class EntityIdEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class ClientEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class ContactEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class PetEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class OfferEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class ProcedureEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class OfferVersionEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class RuleSetEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class GroomerEnvelope
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

internal sealed class AppointmentEnvelope
{
    public Guid Id { get; set; }
    public Guid? BookingRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int VersionNo { get; set; }
    public AppointmentItemEnvelope[] Items { get; set; } = [];
}

internal sealed class AppointmentItemEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class PagedAppointmentEnvelope
{
    public AppointmentListEnvelope[] Items { get; set; } = [];
}

internal sealed class AppointmentListEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class BookingRequestEnvelope
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
}

internal sealed class PagedBookingRequestEnvelope
{
    public BookingRequestListEnvelope[] Items { get; set; } = [];
}

internal sealed class BookingRequestListEnvelope
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
}

internal sealed class AvailabilityEnvelope
{
    public bool IsAvailable { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public int CheckedReservedMinutes { get; set; }
    public string[] Reasons { get; set; } = [];
}

internal sealed class GroomerScheduleEnvelope
{
    public TimeBlockEnvelope[] TimeBlocks { get; set; } = [];
    public AvailabilityWindowEnvelope[] AvailabilityWindows { get; set; } = [];
}

internal sealed class TimeBlockEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class AvailabilityWindowEnvelope
{
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
}

internal sealed class PreviewQuoteEnvelope
{
    public PriceSnapshotEnvelope PriceSnapshot { get; set; } = new();
    public DurationSnapshotEnvelope DurationSnapshot { get; set; } = new();
}

internal sealed class PriceSnapshotEnvelope
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public PriceLineEnvelope[] Lines { get; set; } = [];
}

internal sealed class PriceLineEnvelope
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

internal sealed class DurationSnapshotEnvelope
{
    public Guid Id { get; set; }
    public int ServiceMinutes { get; set; }
    public int ReservedMinutes { get; set; }
    public DurationLineEnvelope[] Lines { get; set; } = [];
}

internal sealed class DurationLineEnvelope
{
    public string Label { get; set; } = string.Empty;
    public string LineType { get; set; } = string.Empty;
}

internal sealed class VisitEnvelope
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal FinalTotalAmount { get; set; }
    public VisitExecutionItemEnvelope[] Items { get; set; } = [];
}

internal sealed class VisitExecutionItemEnvelope
{
    public Guid Id { get; set; }
    public VisitExpectedComponentEnvelope[] ExpectedComponents { get; set; } = [];
    public VisitPerformedProcedureEnvelope[] PerformedProcedures { get; set; } = [];
    public VisitSkippedComponentEnvelope[] SkippedComponents { get; set; } = [];
}

internal sealed class VisitExpectedComponentEnvelope
{
    public Guid Id { get; set; }
    public Guid ProcedureId { get; set; }
}

internal sealed class VisitPerformedProcedureEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class VisitSkippedComponentEnvelope
{
    public Guid Id { get; set; }
}

internal sealed class AuditTrailEnvelope
{
    public AuditTrailItemEnvelope[] Items { get; set; } = [];
}

internal sealed class AuditTrailItemEnvelope
{
    public string ActionCode { get; set; } = string.Empty;
}

internal sealed class AccessAuditEnvelope
{
    public AccessAuditItemEnvelope[] Items { get; set; } = [];
}

internal sealed class AccessAuditItemEnvelope
{
    public string ActionCode { get; set; } = string.Empty;
}
