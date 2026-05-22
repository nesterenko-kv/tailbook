using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Staff.Application.Staff.Commands;

public sealed record AddGroomerTimeBlockUseCaseCommand(
    Guid GroomerId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string ReasonCode,
    string? Notes) : ICommand<ErrorOr<TimeBlockView>>;