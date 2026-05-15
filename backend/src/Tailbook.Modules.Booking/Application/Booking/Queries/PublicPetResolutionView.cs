using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed record PublicPetResolutionView(
    PetQuoteProfile Pet,
    bool UsesSavedPet);