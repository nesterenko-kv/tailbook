namespace Tailbook.Performance.Tests.Benchmarks;

[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5)]
[MemoryDiagnoser]
public class DomainModelBenchmarks
{
    private Guid _petId;
    private Guid _groomerId;
    private Guid _actorUserId;
    private DateTimeOffset _utcNow;
    private IReadOnlyCollection<AppointmentItemDraft> _items = null!;
    private BookingPeriod _period = null!;

    [GlobalSetup]
    public void Setup()
    {
        _petId = Guid.NewGuid();
        _groomerId = Guid.NewGuid();
        _actorUserId = Guid.NewGuid();
        _utcNow = DateTimeOffset.UtcNow;
        _items = new List<AppointmentItemDraft>
        {
            new("Service", Guid.NewGuid(), Guid.NewGuid(), "BRONZE", "Bronze Groom", 1, Guid.NewGuid(), Guid.NewGuid())
        };
        _period = BookingPeriod.Create(_utcNow, _utcNow.AddHours(2)).Value;
    }

    [Benchmark]
    public ErrorOr<Appointment> Create_Appointment_Aggregate() => Appointment.Create(
            Guid.NewGuid(), null, _petId, _groomerId,
            _period, _items, _actorUserId, _utcNow);

    [Benchmark]
    public ErrorOr<BookingPeriod> Create_BookingPeriod() => BookingPeriod.Create(_utcNow, _utcNow.AddHours(1));

    [Benchmark]
    public AppointmentItemDraft Create_AppointmentItemDraft() => new AppointmentItemDraft(
            "Service", Guid.NewGuid(), Guid.NewGuid(),
            "SILVER", "Silver Groom", 1,
            Guid.NewGuid(), Guid.NewGuid());
}
