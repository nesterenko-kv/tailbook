using System.Text.Json.Serialization;

namespace Tailbook.Api.Tests.TestSupport.Models;

public sealed record PagedAppointmentEnvelope
{
    [JsonPropertyName("items")]
    public IReadOnlyCollection<AppointmentSummaryItem> Items { get; set; } = [];

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

public sealed record AppointmentSummaryItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("bookingRequestId")]
    public Guid? BookingRequestId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public sealed record PagedBookingRequestEnvelope
{
    [JsonPropertyName("items")]
    public IReadOnlyCollection<BookingRequestSummaryItem> Items { get; set; } = [];

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

public sealed record BookingRequestSummaryItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public sealed record PetCatalogSelection(
    string DogAnimalTypeCode,
    Guid DogAnimalTypeId,
    string DoubleCoatCode,
    string LargeSizeCode,
    Guid SamoyedBreedId);

public sealed class PetCatalogEnvelope
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

public sealed class CatalogAnimalTypeEnvelope
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

public sealed class CatalogBreedEnvelope
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

public sealed class CatalogCoatTypeEnvelope
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

public sealed class CatalogSizeCategoryEnvelope
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
}

public sealed class EntityIdEnvelope
{
    public Guid Id { get; set; }
}

public sealed class ClientEnvelope
{
    public Guid Id { get; set; }
}

public sealed class ContactEnvelope
{
    public Guid Id { get; set; }
}

public sealed class PetEnvelope
{
    public Guid Id { get; set; }
}

public sealed class OfferEnvelope
{
    public Guid Id { get; set; }
}

public sealed class ProcedureEnvelope
{
    public Guid Id { get; set; }
}

public sealed class OfferVersionEnvelope
{
    public Guid Id { get; set; }
}

public sealed class RuleSetEnvelope
{
    public Guid Id { get; set; }
}

public sealed class GroomerEnvelope
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class AppointmentEnvelope
{
    public Guid Id { get; set; }
    public Guid? BookingRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int VersionNo { get; set; }
    public AppointmentItemEnvelope[] Items { get; set; } = [];
}

public sealed class AppointmentItemEnvelope
{
    public Guid Id { get; set; }
}

public sealed class AppointmentListEnvelope
{
    public Guid Id { get; set; }
}

public sealed class BookingRequestEnvelope
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class BookingRequestListEnvelope
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class AvailabilityEnvelope
{
    public bool IsAvailable { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public int CheckedReservedMinutes { get; set; }
    public string[] Reasons { get; set; } = [];
}

public sealed class GroomerScheduleEnvelope
{
    public TimeBlockEnvelope[] TimeBlocks { get; set; } = [];
    public AvailabilityWindowEnvelope[] AvailabilityWindows { get; set; } = [];
}

public sealed class TimeBlockEnvelope
{
    public Guid Id { get; set; }
}

public sealed class AvailabilityWindowEnvelope
{
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
}

public sealed class PreviewQuoteEnvelope
{
    public PriceSnapshotEnvelope PriceSnapshot { get; set; } = new();
    public DurationSnapshotEnvelope DurationSnapshot { get; set; } = new();
}

public sealed class PriceSnapshotEnvelope
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public PriceLineEnvelope[] Lines { get; set; } = [];
}

public sealed class PriceLineEnvelope
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class DurationSnapshotEnvelope
{
    public Guid Id { get; set; }
    public int ServiceMinutes { get; set; }
    public int ReservedMinutes { get; set; }
    public DurationLineEnvelope[] Lines { get; set; } = [];
}

public sealed class DurationLineEnvelope
{
    public string Label { get; set; } = string.Empty;
    public string LineType { get; set; } = string.Empty;
}

public sealed class VisitEnvelope
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal FinalTotalAmount { get; set; }
    public VisitExecutionItemEnvelope[] Items { get; set; } = [];
}

public sealed class VisitExecutionItemEnvelope
{
    public Guid Id { get; set; }
    public VisitExpectedComponentEnvelope[] ExpectedComponents { get; set; } = [];
    public VisitPerformedProcedureEnvelope[] PerformedProcedures { get; set; } = [];
    public VisitSkippedComponentEnvelope[] SkippedComponents { get; set; } = [];
}

public sealed class VisitExpectedComponentEnvelope
{
    public Guid Id { get; set; }
    public Guid ProcedureId { get; set; }
}

public sealed class VisitPerformedProcedureEnvelope
{
    public Guid Id { get; set; }
}

public sealed class VisitSkippedComponentEnvelope
{
    public Guid Id { get; set; }
}

public sealed class AuditTrailEnvelope
{
    public AuditTrailItemEnvelope[] Items { get; set; } = [];
}

public sealed class AuditTrailItemEnvelope
{
    public string ActionCode { get; set; } = string.Empty;
}

public sealed class AccessAuditEnvelope
{
    public AccessAuditItemEnvelope[] Items { get; set; } = [];
}

public sealed class AccessAuditItemEnvelope
{
    public string ActionCode { get; set; } = string.Empty;
}
