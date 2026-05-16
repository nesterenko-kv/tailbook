using Tailbook.Api.Tests.TestSupport.Models;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Assertions;

public static class AvailabilityAssertions
{
    extension(AvailabilityEnvelope availability)
    {
        public void ShouldBeAvailableUntil(DateTimeOffset expectedEndAt)
        {
            Assert.True(availability.IsAvailable);
            Assert.Equal(expectedEndAt, availability.EndAt);
        }

        public void ShouldBeUnavailableBecause(string reasonText)
        {
            Assert.False(availability.IsAvailable);
            Assert.Contains(availability.Reasons, x => x.Contains(reasonText, StringComparison.OrdinalIgnoreCase));
        }
    }

    extension(GroomerAvailabilityResult availability)
    {
        public void ShouldBeUnavailableBecause(string reason)
        {
            Assert.False(availability.IsAvailable);
            Assert.Contains(availability.Reasons, x => x.Contains(reason, StringComparison.OrdinalIgnoreCase));
        }
    }
}
