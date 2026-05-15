using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Staff.Application.Staff.Commands;

public sealed record CreateGroomerUseCaseCommand(
    string DisplayName,
    Guid? UserId) : ICommand<ErrorOr<GroomerDetailView>>;
