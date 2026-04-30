using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Staff.Application.Staff.Commands;

public sealed record CreateGroomerUseCaseCommand(
    string DisplayName,
    Guid? UserId) : ICommand<ErrorOr<GroomerDetailView>>;

public sealed record AddGroomerCapabilityUseCaseCommand(
    AddGroomerCapabilityCommand Capability) : ICommand<ErrorOr<GroomerCapabilityView>>;

public sealed record UpsertGroomerWorkingScheduleUseCaseCommand(
    Guid GroomerId,
    int Weekday,
    string StartLocalTime,
    string EndLocalTime) : ICommand<ErrorOr<WorkingScheduleView>>;

public sealed record AddGroomerTimeBlockUseCaseCommand(
    Guid GroomerId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string ReasonCode,
    string? Notes) : ICommand<ErrorOr<TimeBlockView>>;
