using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Staff.Infrastructure.Services;

public sealed class StaffCommandHandlers(StaffUseCases useCases)
    : ICommandHandler<CreateGroomerUseCaseCommand, ErrorOr<GroomerDetailView>>,
        ICommandHandler<AddGroomerCapabilityUseCaseCommand, ErrorOr<GroomerCapabilityView>>,
        ICommandHandler<UpsertGroomerWorkingScheduleUseCaseCommand, ErrorOr<WorkingScheduleView>>,
        ICommandHandler<AddGroomerTimeBlockUseCaseCommand, ErrorOr<TimeBlockView>>
{
    public Task<ErrorOr<GroomerDetailView>> ExecuteAsync(CreateGroomerUseCaseCommand command, CancellationToken ct = default)
    {
        return useCases.CreateGroomerAsync(command.DisplayName, command.UserId, ct);
    }

    public Task<ErrorOr<GroomerCapabilityView>> ExecuteAsync(AddGroomerCapabilityUseCaseCommand command, CancellationToken ct = default)
    {
        return useCases.AddCapabilityAsync(command.Capability, ct);
    }

    public Task<ErrorOr<WorkingScheduleView>> ExecuteAsync(UpsertGroomerWorkingScheduleUseCaseCommand command, CancellationToken ct = default)
    {
        return useCases.UpsertWorkingScheduleAsync(command.GroomerId, command.Weekday, command.StartLocalTime, command.EndLocalTime, ct);
    }

    public Task<ErrorOr<TimeBlockView>> ExecuteAsync(AddGroomerTimeBlockUseCaseCommand command, CancellationToken ct = default)
    {
        return useCases.AddTimeBlockAsync(command.GroomerId, command.StartAtUtc, command.EndAtUtc, command.ReasonCode, command.Notes, ct);
    }
}
