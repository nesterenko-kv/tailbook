using Tailbook.Api.Tests.TestSupport.Models;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Assertions;

internal static class AvailabilityAssertions
{
    extension(AvailabilityEnvelope availability)
    {
        internal void ShouldBeAvailableUntil(DateTimeOffset expectedEndAt)
        {
            Assert.True(availability.IsAvailable);
            Assert.Equal(expectedEndAt, availability.EndAt);
        }

        internal void ShouldBeUnavailableBecause(string reasonText)
        {
            Assert.False(availability.IsAvailable);
            Assert.Contains(availability.Reasons, x => x.Contains(reasonText, StringComparison.OrdinalIgnoreCase));
        }
    }
}
