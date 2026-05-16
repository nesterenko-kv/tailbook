using Tailbook.Api.Tests.TestSupport.Models;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Assertions;

public static class AvailabilityAssertions
{
    extension(GroomerAvailabilityResult availability)
    {
        public void ShouldBeUnavailableBecause(string reason)
        {
            Assert.False(availability.IsAvailable);
            Assert.Contains(availability.Reasons, x => x.Contains(reason, StringComparison.OrdinalIgnoreCase));
        }
    }
}
