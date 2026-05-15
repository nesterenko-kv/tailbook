using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Staff.Application.Staff.Commands;

public sealed record UpsertGroomerWorkingScheduleUseCaseCommand(
    Guid GroomerId,
    int Weekday,
    string StartLocalTime,
    string EndLocalTime) : ICommand<ErrorOr<WorkingScheduleView>>;