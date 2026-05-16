using System.Globalization;
using System.Text.Json;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Modules.Booking.Contracts;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class BookingAppointmentApplicationTests
{
    [Fact]
    public void Time_normalizer_preserves_utc_input()
    {
        var input = Utc("2026-04-22T07:00:00Z");

        var normalized = AssertSuccess(BookingTimeInputNormalizer.AssumeUtc(input, nameof(input)));

        Assert.Equal(TimeSpan.Zero, normalized.Offset);
        Assert.Equal(input, normalized);
    }

    [Fact]
    public void Time_normalizer_preserves_legacy_utc_wall_clock_input()
    {
        var input = UtcWallClock("2026-04-22T07:00:00");

        var normalized = AssertSuccess(BookingTimeInputNormalizer.AssumeUtc(input, nameof(input)));

        Assert.Equal(TimeSpan.Zero, normalized.Offset);
        Assert.Equal(input, normalized);
    }

    [Fact]
    public void Time_normalizer_converts_offset_input_to_utc()
    {
        var input = DateTimeOffset.Parse("2026-04-22T10:00:00+03:00", CultureInfo.InvariantCulture);

        var normalized = AssertSuccess(BookingTimeInputNormalizer.AssumeUtc(input, nameof(input)));

        Assert.Equal(TimeSpan.Zero, normalized.Offset);
        Assert.Equal(Utc("2026-04-22T07:00:00Z"), normalized);
    }

    [Fact]
    public void Time_normalizer_delegates_period_order_validation_to_booking_period()
    {
        AssertError(BookingTimeInputNormalizer.CreatePeriod(
            UtcWallClock("2026-04-22T08:30:00"),
            UtcWallClock("2026-04-22T07:00:00")));
    }

    [Fact]
    public void Time_normalizer_returns_all_required_period_errors()
    {
        AssertErrorCodes(
            BookingTimeInputNormalizer.CreatePeriod(default, default),
            "Booking.startAtRequired",
            "Booking.endAtRequired");
    }

    [Fact]
    public async Task Create_and_reschedule_use_the_same_legacy_utc_normalization_path()
    {
        await using var harness = await BookingApplicationHarness.CreateAsync(seedAppointment: false);
        var createStart = UtcWallClock("2026-04-22T07:00:00");
        var offerId = Guid.NewGuid();

        var created = await harness.CreateAppointmentHandler.ExecuteAsync(
            new CreateAppointmentUseCaseCommand(
                harness.PetId,
                harness.GroomerId,
                createStart,
                [new CreateAppointmentItemData(offerId, "Package")],
                Guid.Empty),
            CancellationToken.None);
        Assert.False(created.IsError);

        Assert.Equal(TimeSpan.Zero, harness.StaffSchedulingService.LastAvailabilityStartAt?.Offset);
        Assert.Equal(createStart, harness.StaffSchedulingService.LastAvailabilityStartAt);
        Assert.Equal(createStart.ToUniversalTime(), created.Value.StartAt);

        var rescheduleStart = UtcWallClock("2026-04-22T09:00:00");
        var rescheduled = await harness.RescheduleAppointmentHandler.ExecuteAsync(
            new RescheduleAppointmentUseCaseCommand(created.Value.Id, harness.GroomerId, rescheduleStart, created.Value.VersionNo, Guid.Empty),
            CancellationToken.None);

        Assert.False(rescheduled.IsError);
        Assert.Equal(TimeSpan.Zero, harness.StaffSchedulingService.LastAvailabilityStartAt?.Offset);
        Assert.Equal(rescheduleStart, harness.StaffSchedulingService.LastAvailabilityStartAt);
        Assert.Equal(rescheduleStart.ToUniversalTime(), rescheduled.Value.StartAt);
    }

    [Fact]
    public async Task Invalid_cancellation_does_not_mutate_appointment_or_publish_event()
    {
        await using var harness = await BookingApplicationHarness.CreateAsync();

        var result = await harness.CancelAppointmentHandler.ExecuteAsync(
            new CancelAppointmentUseCaseCommand(harness.AppointmentId!.Value, 1, " ", null, Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, error => error.Code == "Booking.CancellationReasonRequired");

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

        var result = await harness.RescheduleAppointmentHandler.ExecuteAsync(
            new RescheduleAppointmentUseCaseCommand(harness.AppointmentId!.Value, harness.GroomerId, UtcWallClock("2026-04-22T09:00:00"), 1, Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, error => error.Code == "Booking.AppointmentSlotUnavailable");

        var appointment = await harness.DbContext.Set<Appointment>().SingleAsync(x => x.Id == harness.AppointmentId);
        Assert.Equal(AppointmentStatusCodes.Confirmed, appointment.Status);
        Assert.Equal(Utc("2026-04-22T07:00:00Z"), appointment.StartAt);
        Assert.Equal(1, appointment.VersionNo);
        Assert.Equal(0, await CountBookingAppointmentEventsAsync(harness.DbContext));
    }

    [Fact]
    public async Task Successful_reschedule_publishes_updated_time_range()
    {
        await using var harness = await BookingApplicationHarness.CreateAsync();
        var startAt = UtcWallClock("2026-04-22T09:00:00");

        await harness.RescheduleAppointmentHandler.ExecuteAsync(
            new RescheduleAppointmentUseCaseCommand(harness.AppointmentId!.Value, harness.GroomerId, startAt, 1, Guid.Empty),
            CancellationToken.None);

        var message = await SingleBookingAppointmentEventAsync(harness.DbContext, "AppointmentRescheduled");
        using var payload = JsonDocument.Parse(message.PayloadJson);
        var expectedStartAt = startAt.ToUniversalTime();
        Assert.Equal(expectedStartAt, payload.RootElement.GetProperty("startAt").GetDateTimeOffset());
        Assert.Equal(expectedStartAt.AddMinutes(90), payload.RootElement.GetProperty("endAt").GetDateTimeOffset());
        Assert.Equal(2, payload.RootElement.GetProperty("versionNo").GetInt32());
    }

    [Fact]
    public async Task Successful_cancellation_publishes_cancelled_status_and_normalized_reason()
    {
        await using var harness = await BookingApplicationHarness.CreateAsync();

        await harness.CancelAppointmentHandler.ExecuteAsync(
            new CancelAppointmentUseCaseCommand(harness.AppointmentId!.Value, 1, " client_request ", "Changed plans", Guid.Empty),
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

    private static async Task<OutboxMessage> SingleBookingAppointmentEventAsync(
        AppDbContext dbContext,
        string eventType)
    {
        return await dbContext.OutboxMessages.SingleAsync(x => x.ModuleCode == "booking" && x.EventType == eventType);
    }

    private static DateTimeOffset Utc(string value)
    {
        return DateTimeOffset.Parse(value).ToUniversalTime();
    }

    private static DateTimeOffset UtcWallClock(string value)
    {
        return new DateTimeOffset(DateTime.Parse(value, CultureInfo.InvariantCulture), TimeSpan.Zero);
    }

    private static T AssertSuccess<T>(ErrorOr<T> result)
    {
        Assert.False(result.IsError, string.Join("; ", result.Errors.Select(error => error.Description)));
        return result.Value;
    }

    private static IReadOnlyList<Error> AssertError<T>(ErrorOr<T> result)
    {
        Assert.True(result.IsError);
        return result.Errors;
    }

    private static void AssertErrorCodes<T>(ErrorOr<T> result, params string[] expectedCodes)
    {
        var codes = AssertError(result).Select(error => error.Code).ToArray();
        Assert.Equal(expectedCodes, codes);
    }

    private sealed class BookingApplicationHarness : IAsyncDisposable
    {
        private BookingApplicationHarness(
            AppDbContext dbContext,
            CreateAppointmentUseCaseCommandHandler createAppointmentHandler,
            RescheduleAppointmentUseCaseCommandHandler rescheduleAppointmentHandler,
            CancelAppointmentUseCaseCommandHandler cancelAppointmentHandler,
            StubStaffSchedulingService staffSchedulingService,
            Guid petId,
            Guid groomerId,
            Guid? appointmentId)
        {
            DbContext = dbContext;
            CreateAppointmentHandler = createAppointmentHandler;
            RescheduleAppointmentHandler = rescheduleAppointmentHandler;
            CancelAppointmentHandler = cancelAppointmentHandler;
            StaffSchedulingService = staffSchedulingService;
            PetId = petId;
            GroomerId = groomerId;
            AppointmentId = appointmentId;
        }

        public AppDbContext DbContext { get; }
        public CreateAppointmentUseCaseCommandHandler CreateAppointmentHandler { get; }
        public RescheduleAppointmentUseCaseCommandHandler RescheduleAppointmentHandler { get; }
        public CancelAppointmentUseCaseCommandHandler CancelAppointmentHandler { get; }
        public StubStaffSchedulingService StaffSchedulingService { get; }
        public Guid PetId { get; }
        public Guid GroomerId { get; }
        public Guid? AppointmentId { get; }

        public static async Task<BookingApplicationHarness> CreateAsync(bool seedAppointment = true)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"booking-application-{Guid.NewGuid():N}")
                .Options;

            var dbContext = TestModelConfiguration.CreateDbContext(options);
            var petId = Guid.NewGuid();
            var groomerId = Guid.NewGuid();
            var petQuoteProfileService = new StubPetQuoteProfileService(petId);
            var staffSchedulingService = new StubStaffSchedulingService();
            var timeProvider = TimeProvider.System;
            var petSummaryReadService = new StubPetSummaryReadService();
            var groomerProfileReadService = new StubGroomerProfileReadService();
            var bookingReadService = new BookingManagementReadService(
                dbContext,
                petQuoteProfileService,
                petSummaryReadService,
                groomerProfileReadService);
            var auditTrailService = new NoOpAuditTrailService();
            var outboxPublisher = new OutboxPublisher(dbContext, timeProvider);
            var snapshotComposer = new BookingSnapshotComposer(
                dbContext,
                petQuoteProfileService,
                new StubCatalogQuoteResolver(),
                staffSchedulingService,
                timeProvider);
            var createAppointmentHandler = new CreateAppointmentUseCaseCommandHandler(
                dbContext,
                bookingReadService,
                snapshotComposer,
                auditTrailService,
                outboxPublisher,
                timeProvider);
            var rescheduleAppointmentHandler = new RescheduleAppointmentUseCaseCommandHandler(
                dbContext,
                bookingReadService,
                staffSchedulingService,
                auditTrailService,
                outboxPublisher,
                timeProvider);
            var cancelAppointmentHandler = new CancelAppointmentUseCaseCommandHandler(
                dbContext,
                bookingReadService,
                auditTrailService,
                outboxPublisher,
                timeProvider);

            Guid? appointmentId = null;
            if (seedAppointment)
            {
                appointmentId = await SeedAppointmentAsync(dbContext, petId, groomerId);
            }

            return new BookingApplicationHarness(
                dbContext,
                createAppointmentHandler,
                rescheduleAppointmentHandler,
                cancelAppointmentHandler,
                staffSchedulingService,
                petId,
                groomerId,
                appointmentId);
        }

        public ValueTask DisposeAsync()
        {
            return DbContext.DisposeAsync();
        }

        private static async Task<Guid> SeedAppointmentAsync(AppDbContext dbContext, Guid petId, Guid groomerId)
        {
            var priceSnapshotId = Guid.NewGuid();
            var durationSnapshotId = Guid.NewGuid();
            var period = BookingTimeInputNormalizer.CreatePeriod(Utc("2026-04-22T07:00:00Z"), Utc("2026-04-22T08:30:00Z"));
            Assert.False(period.IsError, string.Join("; ", period.Errors.Select(error => error.Description)));

            var appointmentResult = Appointment.Create(
                Guid.NewGuid(),
                null,
                petId,
                groomerId,
                period.Value,
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
            Assert.False(appointmentResult.IsError, string.Join("; ", appointmentResult.Errors.Select(error => error.Description)));
            var appointment = appointmentResult.Value;

            dbContext.Set<PriceSnapshot>().Add(new PriceSnapshot
            {
                Id = priceSnapshotId,
                SnapshotType = SnapshotTypeCodes.AppointmentEstimate,
                Currency = "UAH",
                TotalAmount = 1000m,
                CreatedAt = Utc("2026-04-21T12:00:00Z")
            });
            dbContext.Set<DurationSnapshot>().Add(new DurationSnapshot
            {
                Id = durationSnapshotId,
                ServiceMinutes = 60,
                ReservedMinutes = 90,
                CreatedAt = Utc("2026-04-21T12:00:00Z")
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

        public Task<ErrorOr<PetQuoteProfile>> CreateAdHocAsync(PetQuoteProfileInput input, CancellationToken cancellationToken)
        {
            return Task.FromResult<ErrorOr<PetQuoteProfile>>(Error.Unexpected("Test.NotSupported", "Ad hoc pets are not supported by this stub."));
        }
    }

    private sealed class StubCatalogQuoteResolver : ICatalogQuoteResolver
    {
        public Task<ErrorOr<CatalogQuoteResolution>> ResolveAsync(
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

            return Task.FromResult<ErrorOr<CatalogQuoteResolution>>(new CatalogQuoteResolution(
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
        public DateTimeOffset? LastAvailabilityStartAt { get; private set; }

        public Task<ErrorOr<ReservedDurationResolution>> ResolveReservedDurationAsync(
            Guid groomerId,
            Guid petId,
            IReadOnlyCollection<Guid> offerIds,
            int baseReservedMinutes,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<ErrorOr<ReservedDurationResolution>>(new ReservedDurationResolution(baseReservedMinutes, baseReservedMinutes, 0, []));
        }

        public Task<ErrorOr<ReservedDurationResolution>> ResolveReservedDurationAsync(
            Guid groomerId,
            PetQuoteProfile pet,
            IReadOnlyCollection<Guid> offerIds,
            int baseReservedMinutes,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<ErrorOr<ReservedDurationResolution>>(new ReservedDurationResolution(baseReservedMinutes, baseReservedMinutes, 0, []));
        }

        public Task<ErrorOr<GroomerAvailabilityCheckResult>> CheckAvailabilityAsync(
            Guid groomerId,
            Guid petId,
            IReadOnlyCollection<Guid> offerIds,
            DateTimeOffset startAt,
            int reservedMinutes,
            Guid? ignoredAppointmentId,
            CancellationToken cancellationToken)
        {
            LastAvailabilityStartAt = startAt;
            return Task.FromResult<ErrorOr<GroomerAvailabilityCheckResult>>(new GroomerAvailabilityCheckResult(
                IsAvailable,
                startAt.AddMinutes(reservedMinutes),
                reservedMinutes,
                IsAvailable ? ["Requested slot is available."] : ["Requested slot is unavailable."]));
        }

        public Task<ErrorOr<GroomerAvailabilityCheckResult>> CheckAvailabilityAsync(
            Guid groomerId,
            PetQuoteProfile pet,
            IReadOnlyCollection<Guid> offerIds,
            DateTimeOffset startAt,
            int reservedMinutes,
            Guid? ignoredAppointmentId,
            CancellationToken cancellationToken)
        {
            return CheckAvailabilityAsync(groomerId, pet.Id, offerIds, startAt, reservedMinutes, ignoredAppointmentId, cancellationToken);
        }

        public Task<IReadOnlyCollection<AvailabilityWindowReadModel>> GetAvailabilityWindowsAsync(
            Guid groomerId,
            DateOnly localDate,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<AvailabilityWindowReadModel>>([]);
        }

        public Task<ErrorOr<GroomerAvailableSlotsReadModel>> GetAvailableSlotsAsync(
            Guid groomerId,
            PetQuoteProfile pet,
            IReadOnlyCollection<Guid> offerIds,
            DateOnly localDate,
            int baseReservedMinutes,
            DateTimeOffset earliestStartAt,
            int slotStepMinutes,
            Guid? ignoredAppointmentId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<ErrorOr<GroomerAvailableSlotsReadModel>>(
                new GroomerAvailableSlotsReadModel(
                    new ReservedDurationResolution(baseReservedMinutes, baseReservedMinutes, 0, []),
                    []));
        }
    }

    private sealed class StubPetSummaryReadService : IPetSummaryReadService
    {
        public Task<PetSummaryReadModel?> GetPetSummaryAsync(Guid petId, CancellationToken cancellationToken)
        {
            return Task.FromResult<PetSummaryReadModel?>(null);
        }

        public Task<IReadOnlyCollection<PetSummaryReadModel>> ListPetSummariesByClientAsync(Guid clientId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<PetSummaryReadModel>>([]);
        }

        public Task<IReadOnlyCollection<Guid>> SearchPetIdsAsync(string? search, int maxResults, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Guid>>([]);
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
        public Task<ErrorOr<GroomerProfileReadModel>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<ErrorOr<GroomerProfileReadModel>>(Error.Forbidden("Staff.GroomerProfileRequired", "Current user is not linked to an active groomer profile."));
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
        public ValueTask RecordAsync(string moduleCode, string entityType, string entityId, string actionCode, Guid? actorUserId, string? beforeJson, string? afterJson, CancellationToken cancellationToken)
        {
            return default;
        }
    }
}
