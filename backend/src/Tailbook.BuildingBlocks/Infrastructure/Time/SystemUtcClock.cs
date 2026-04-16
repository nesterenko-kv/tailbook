using Tailbook.SharedKernel.Abstractions;

namespace Tailbook.BuildingBlocks.Infrastructure.Time;

public sealed class SystemUtcClock : IUtcClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
