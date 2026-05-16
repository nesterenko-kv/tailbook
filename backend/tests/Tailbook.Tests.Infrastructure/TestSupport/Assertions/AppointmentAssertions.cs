using Tailbook.Api.Tests.TestSupport.Models;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Assertions;

public static class AppointmentAssertions
{
    extension(AppointmentSummaryItem appointment)
    {
        public void ShouldBeConvertedFrom(Guid bookingRequestId)
        {
            Assert.Equal(bookingRequestId, appointment.BookingRequestId);
        }

        public void ShouldHaveStatus(string expectedStatus, int versionNo)
        {
            Assert.Equal(expectedStatus, appointment.Status);
        }
    }
}
