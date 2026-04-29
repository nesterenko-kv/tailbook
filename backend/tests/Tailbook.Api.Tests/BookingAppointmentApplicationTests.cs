using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking;
using Tailbook.Modules.Booking.Application;
using Tailbook.Modules.Booking.Contracts;
using Tailbook.Modules.Booking.Domain;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class BookingAppointmentApplicationTests
{
    [Fact]
    public void Time_normalizer_preserves_utc_input()
    {
        var input = Utc("2026-04-22T07:00:00Z");

        var normalized = BookingTimeInputNormalizer.AssumeUtc(input, nameof(input));

        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
        Assert.Equal(input, normalized);
    }

    [Fact]
    public void Time_normalizer_treats_unspecified_input_as_utc_for_legacy_api_compatibility()
    {
        var input = Unspecified("2026-04-22T07:00:00");

        var normalized = BookingTimeInputNormalizer.AssumeUtc(input, nameof(input));

        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
        Assert.Equal(input.Ticks, normalized.Ticks);
    }

    [Fact]
    public void Time_normalizer_treats_local_input_as_utc_wall_clock_for_legacy_api_compatibility()
    {
        var input = DateTime.SpecifyKind(DateTime.Parse("2026-04-22T07:00:00"), DateTimeKind.Local);

        var normalized = BookingTimeInputNormalizer.AssumeUtc(input, nameof(input));

        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
        Assert.Equal(input.Ticks, normalized.Ticks);
    }

    [Fact]
    public void Time_normalizer_delegates_period_order_validation_to_booking_period()
    {
        Assert.Throws<InvalidOperationException>(() => BookingTimeInputNormalizer.CreatePeriod(
            Unspecified("2026-04-22T08:30:00"),
            Unspecified("2026-04-22T07:00:00")));
    }

    [Fact]
    public async Task Create_and_reschedule_use_the_same_legacy_utc_normalization_path()
    {
        await using var harness = await BookingApplicationHarness.CreateAsync(seedAppointment: false);
        var createStart = Unspecified("2026-04-22T07:00:00");
        var offerId = Guid.NewGuid();

        var created = await harness.Queries.CreateAppointmentAsync(
            new CreateAppointmentCommand(
                harness.PetId,
                harness.GroomerId,
                createStart,
                [new CreateAppointmentItemCommand(offerId, "Package")]),
            null,
            CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, harness.StaffSchedulingService.LastAvailabilityStartAtUtc?.Kind);
        Assert.Equal(createStart.Ticks, harness.StaffSchedulingService.LastAvailabilityStartAtUtc?.Ticks);
        Assert.Equal(DateTime.SpecifyKind(createStart, DateTimeKind.Utc), created.StartAtUtc);

        var rescheduleStart = Unspecified("2026-04-22T09:00:00");
        var rescheduled = await harness.Queries.RescheduleAppointmentAsync(
            new RescheduleAppointmentCommand(created.Id, harness.GroomerId, rescheduleStart, created.VersionNo),
            null,
            CancellationToken.None);

        Assert.NotNull(rescheduled);
        Assert.Equal(DateTimeKind.Utc, harness.StaffSchedulingService.LastAvailabilityStartAtUtc?.Kind);
        Assert.Equal(rescheduleStart.Ticks, harness.StaffSchedulingService.LastAvailabilityStartAtUtc?.Ticks);
        Assert.Equal(DateTime.SpecifyKind(rescheduleStart, DateTimeKind.Utc), rescheduled!.StartAtUtc);
    }

    [Fact]
    public async Task Invalid_cancellation_does_not_mutate_appointment_or_publish_event()
    {
        await using var harness = await BookingApplicationHarness.CreateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Queries.CancelAppointmentAsync(
            new CancelAppointmentCommand(harness.AppointmentId!.Value, 1, " ", null),
            null,
            CancellationToken.None));

        var appointment = await harness.DbContext.Set<Appointment>().SingleAsync(x => x.Id == harness.AppointmentId);
        Assert.Equal(AppointmentStatusCodes.Confirmed, appointment.Status);
        Assert.Equal(1, appointment.VersionNo);
        Assert.Equal(0, await CountBookingAppointmentEventsAsync(harness.DbContext));
    }

    [Fact]
    public async Task Invalid_reschedule_does_not_mutate_appointment_or_publish_event()
    {
        await using var harness = await BookingApplicationHarness.CreateAsync();
        harness.StaffSchedulingService.IsAvailable = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Queries.RescheduleAppointmentAsync(
            new RescheduleAppointmentCommand(harness.AppointmentId!.Value, harness.GroomerId, Unspecified("2026-04-22T09:00:00"), 1),
            null,
            CancellationToken.None));

        var appointment = await harness.DbContext.Set<Appointment>().SingleAsync(x => x.Id == harness.AppointmentId);
        Assert.Equal(AppointmentStatusCodes.Confirmed, appointment.Status);
        Assert.Equal(Utc("2026-04-22T07:00:00Z"), appointment.StartAtUtc);
        Assert.Equal(1, appointment.VersionNo);
        Assert.Equal(0, await CountBookingAppointmentEventsAsync(harness.DbContext));
    }

    [Fact]
    public async Task Successful_reschedule_publishes_updated_time_range()
    {
        await using var harness = await BookingApplicationHarness.CreateAsync();
        var startAtUtc = Unspecified("2026-04-22T09:00:00");

        await harness.Queries.RescheduleAppointmentAsync(
            new RescheduleAppointmentCommand(harness.AppointmentId!.Value, harness.GroomerId, startAtUtc, 1),
            null,
            CancellationToken.None);

        var message = await SingleBookingAppointmentEventAsync(harness.DbContext, "AppointmentRescheduled");
        using var payload = JsonDocument.Parse(message.PayloadJson);
        var expectedStartAtUtc = DateTime.SpecifyKind(startAtUtc, DateTimeKind.Utc);
        Assert.Equal(expectedStartAtUtc, payload.RootElement.GetProperty("startAtUtc").GetDateTime());
        Assert.Equal(expectedStartAtUtc.AddMinutes(90), payload.RootElement.GetProperty("endAtUtc").GetDateTime());
        Assert.Equal(2, payload.RootElement.GetProperty("versionNo").GetInt32());
    }

    [Fact]
    public async Task Successful_cancellation_publishes_cancelled_status_and_normalized_reason()
    {
        await using var harness = await BookingApplicationHarness.CreateAsync();

        await harness.Queries.CancelAppointmentAsync(
            new CancelAppointmentCommand(harness.AppointmentId!.Value, 1, " client_request ", "Changed plans"),
            null,
            CancellationToken.None);

        var message = await SingleBookingAppointmentEventAsync(harness.DbContext, "AppointmentCancelled");
        using var payload = JsonDocument.Parse(message.PayloadJson);
        Assert.Equal(AppointmentStatusCodes.Cancelled, payload.RootElement.GetProperty("status").GetString());
        Assert.Equal("CLIENT_REQUEST", payload.RootElement.GetProperty("reasonCode").GetString());
        Assert.Equal(2, payload.RootElement.GetProperty("versionNo").GetInt32());
    }

    private static Task<int> CountBookingAppointmentEventsAsync(AppDbContext dbContext)
    {
        return dbContext.OutboxMessages
            .CountAsync(x => x.ModuleCode == "booking" && x.EventType.StartsWith("Appointment"));
    }

    private static async Task<Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration.OutboxMessage> SingleBookingAppointmentEventAsync(
        AppDbContext dbContext,
        string eventType)
    {
        return await dbContext.OutboxMessages.SingleAsync(x => x.ModuleCode == "booking" && x.EventType == eventType);
    }

    private static DateTime Utc(string value)
    {
        return DateTime.Parse(value).ToUniversalTime();
    }

    private static DateTime Unspecified(string value)
    {
        return DateTime.SpecifyKind(DateTime.Parse(value), DateTimeKind.Unspecified);
    }

    private sealed class BookingApplicationHarness : IAsyncDisposable
    {
        private BookingApplicationHarness(
            AppDbContext dbContext,
            BookingManagementQueries queries,
            StubStaffSchedulingService staffSchedulingService,
            Guid petId,
            Guid groomerId,
            Guid? appointmentId)
        {
            DbContext = dbContext;
            Queries = queries;
            StaffSchedulingService = staffSchedulingService;
            PetId = petId;
            GroomerId = groomerId;
            AppointmentId = appointmentId;
        }

        public AppDbContext DbContext { get; }
        public BookingManagementQueries Queries { get; }
        public StubStaffSchedulingService StaffSchedulingService { get; }
        public Guid PetId { get; }
        public Guid GroomerId { get; }
        public Guid? AppointmentId { get; }

        public static async Task<BookingApplicationHarness> CreateAsync(bool seedAppointment = true)
        {
            new BookingModule().ConfigurePersistence();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"booking-application-{Guid.NewGuid():N}")
                .Options;

            var dbContext = new AppDbContext(options);
            var petId = Guid.NewGuid();
            var groomerId = Guid.NewGuid();
            var petQuoteProfileService = new StubPetQuoteProfileService(petId);
            var staffSchedulingService = new StubStaffSchedulingService();
            var queries = new BookingManagementQueries(
                dbContext,
                new BookingSnapshotComposer(
                    dbContext,
                    petQuoteProfileService,
                    new StubCatalogQuoteResolver(),
                    staffSchedulingService),
                petQuoteProfileService,
                new StubPetSummaryReadService(),
                new StubClientReferenceValidationService(),
                new StubContactReferenceValidationService(),
                new StubOfferReferenceValidationService(),
                staffSchedulingService,
                new StubGroomerProfileReadService(),
                new NoOpAuditTrailService(),
                new OutboxPublisher(dbContext));

            Guid? appointmentId = null;
            if (seedAppointment)
            {
                appointmentId = await SeedAppointmentAsync(dbContext, petId, groomerId);
            }

            return new BookingApplicationHarness(dbContext, queries, staffSchedulingService, petId, groomerId, appointmentId);
        }

        public ValueTask DisposeAsync()
        {
            return DbContext.DisposeAsync();
        }

        private static async Task<Guid> SeedAppointmentAsync(AppDbContext dbContext, Guid petId, Guid groomerId)
        {
            var priceSnapshotId = Guid.NewGuid();
            var durationSnapshotId = Guid.NewGuid();
            var appointment = Appointment.Create(
                Guid.NewGuid(),
                null,
                petId,
                groomerId,
                BookingTimeInputNormalizer.CreatePeriod(Utc("2026-04-22T07:00:00Z"), Utc("2026-04-22T08:30:00Z")),
                [
                    new AppointmentItemDraft(
                        "Package",
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "BASIC",
                        "Basic Groom",
                        1,
                        priceSnapshotId,
                        durationSnapshotId)
                ],
                null,
                Utc("2026-04-21T12:00:00Z"));

            dbContext.Set<PriceSnapshot>().Add(new PriceSnapshot
            {
                Id = priceSnapshotId,
                SnapshotType = SnapshotTypeCodes.AppointmentEstimate,
                Currency = "UAH",
                TotalAmount = 1000m,
                CreatedAtUtc = Utc("2026-04-21T12:00:00Z")
            });
            dbContext.Set<DurationSnapshot>().Add(new DurationSnapshot
            {
                Id = durationSnapshotId,
                ServiceMinutes = 60,
                ReservedMinutes = 90,
                CreatedAtUtc = Utc("2026-04-21T12:00:00Z")
            });
            dbContext.Set<Appointment>().Add(appointment);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            return appointment.Id;
        }
    }

    private sealed class StubPetQuoteProfileService(Guid petId) : IPetQuoteProfileService
    {
        public Task<PetQuoteProfile?> GetPetAsync(Guid requestedPetId, CancellationToken cancellationToken)
        {
            var pet = requestedPetId == petId
                ? new PetQuoteProfile(
                    petId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "DOG",
                    "Dog",
                    Guid.NewGuid(),
                    "SAMOYED",
                    "Samoyed",
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null)
                : null;

            return Task.FromResult(pet);
        }

        public Task<PetQuoteProfile> CreateAdHocAsync(PetQuoteProfileInput input, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubCatalogQuoteResolver : ICatalogQuoteResolver
    {
        public Task<CatalogQuoteResolution> ResolveAsync(
            PetQuoteProfile pet,
            IReadOnlyCollection<QuotePreviewCatalogItem> items,
            CancellationToken cancellationToken)
        {
            var resolvedItems = items.Select(x => new CatalogResolvedQuoteItem(
                x.OfferId,
                Guid.NewGuid(),
                "BASIC",
                x.ItemType ?? "Package",
                "Basic Groom",
                1000m,
                60,
                90)).ToArray();

            return Task.FromResult(new CatalogQuoteResolution(
                null,
                null,
                "UAH",
                resolvedItems.Sum(x => x.PriceAmount),
                resolvedItems.Sum(x => x.ServiceMinutes),
                resolvedItems.Sum(x => x.ReservedMinutes),
                resolvedItems.Select((x, index) => new CatalogQuotePriceLine(
                    x.OfferId,
                    x.OfferVersionId,
                    "Base",
                    x.DisplayName,
                    x.PriceAmount,
                    null,
                    index + 1)).ToArray(),
                resolvedItems.Select((x, index) => new CatalogQuoteDurationLine(
                    x.OfferId,
                    x.OfferVersionId,
                    "Base",
                    x.DisplayName,
                    x.ReservedMinutes,
                    null,
                    index + 1)).ToArray(),
                resolvedItems));
        }
    }

    private sealed class StubStaffSchedulingService : IStaffSchedulingService
    {
        public bool IsAvailable { get; set; } = true;
        public DateTime? LastAvailabilityStartAtUtc { get; private set; }

        public Task<ReservedDurationResolution> ResolveReservedDurationAsync(
            Guid groomerId,
            Guid petId,
            IReadOnlyCollection<Guid> offerIds,
            int baseReservedMinutes,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ReservedDurationResolution(baseReservedMinutes, baseReservedMinutes, 0, []));
        }

        public Task<ReservedDurationResolution> ResolveReservedDurationAsync(
            Guid groomerId,
            PetQuoteProfile pet,
            IReadOnlyCollection<Guid> offerIds,
            int baseReservedMinutes,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ReservedDurationResolution(baseReservedMinutes, baseReservedMinutes, 0, []));
        }

        public Task<GroomerAvailabilityCheckResult> CheckAvailabilityAsync(
            Guid groomerId,
            Guid petId,
            IReadOnlyCollection<Guid> offerIds,
            DateTime startAtUtc,
            int reservedMinutes,
            Guid? ignoredAppointmentId,
            CancellationToken cancellationToken)
        {
            LastAvailabilityStartAtUtc = startAtUtc;
            return Task.FromResult(new GroomerAvailabilityCheckResult(
                IsAvailable,
                startAtUtc.AddMinutes(reservedMinutes),
                reservedMinutes,
                IsAvailable ? ["Requested slot is available."] : ["Requested slot is unavailable."]));
        }

        public Task<GroomerAvailabilityCheckResult> CheckAvailabilityAsync(
            Guid groomerId,
            PetQuoteProfile pet,
            IReadOnlyCollection<Guid> offerIds,
            DateTime startAtUtc,
            int reservedMinutes,
            Guid? ignoredAppointmentId,
            CancellationToken cancellationToken)
        {
            return CheckAvailabilityAsync(groomerId, pet.Id, offerIds, startAtUtc, reservedMinutes, ignoredAppointmentId, cancellationToken);
        }

        public Task<IReadOnlyCollection<AvailabilityWindowReadModel>> GetAvailabilityWindowsAsync(
            Guid groomerId,
            DateOnly localDate,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<AvailabilityWindowReadModel>>([]);
        }
    }

    private sealed class StubPetSummaryReadService : IPetSummaryReadService
    {
        public Task<PetSummaryReadModel?> GetPetSummaryAsync(Guid petId, CancellationToken cancellationToken)
        {
            return Task.FromResult<PetSummaryReadModel?>(null);
        }
    }

    private sealed class StubClientReferenceValidationService : IClientReferenceValidationService
    {
        public Task<bool> ExistsAsync(Guid clientId, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class StubContactReferenceValidationService : IContactReferenceValidationService
    {
        public Task<bool> ExistsAsync(Guid contactId, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class StubOfferReferenceValidationService : IOfferReferenceValidationService
    {
        public Task<bool> ExistsAsync(Guid offerId, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class StubGroomerProfileReadService : IGroomerProfileReadService
    {
        public Task<GroomerProfileReadModel?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<GroomerProfileReadModel?>(null);
        }

        public Task<GroomerProfileReadModel?> GetByGroomerIdAsync(Guid groomerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<GroomerProfileReadModel?>(null);
        }

        public Task<IReadOnlyCollection<GroomerProfileReadModel>> ListActiveAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<GroomerProfileReadModel>>([]);
        }
    }

    private sealed class NoOpAuditTrailService : IAuditTrailService
    {
        public Task RecordAsync(string moduleCode, string entityType, string entityId, string actionCode, Guid? actorUserId, string? beforeJson, string? afterJson, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
