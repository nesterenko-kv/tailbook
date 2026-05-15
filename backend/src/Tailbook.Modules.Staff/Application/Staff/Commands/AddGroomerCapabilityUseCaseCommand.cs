using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Staff.Application.Staff.Commands;

public sealed record AddGroomerCapabilityUseCaseCommand(
    Guid GroomerId,
    Guid? AnimalTypeId,
    Guid? BreedId,
    Guid? BreedGroupId,
    Guid? CoatTypeId,
    Guid? SizeCategoryId,
    Guid? OfferId,
    string CapabilityMode,
    int ReservedDurationModifierMinutes,
    string? Notes) : ICommand<ErrorOr<GroomerCapabilityView>>;
