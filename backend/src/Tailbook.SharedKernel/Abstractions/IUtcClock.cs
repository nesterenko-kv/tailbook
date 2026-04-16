namespace Tailbook.SharedKernel.Abstractions;

public interface IUtcClock
{
    DateTime UtcNow { get; }
}
