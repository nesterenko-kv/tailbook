namespace Tailbook.Api.Tests.TestSupport.Models;

public sealed record GroomerAvailabilityResult
{
    public bool IsAvailable { get; set; }
    public IReadOnlyCollection<string> Reasons { get; set; } = [];
}
