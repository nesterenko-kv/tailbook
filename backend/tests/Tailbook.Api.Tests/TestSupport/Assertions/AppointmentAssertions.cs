using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Tailbook.Api.Tests.TestSupport.Models;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Assertions;

internal static class AppointmentAssertions
{
    extension(AppointmentEnvelope appointment)
    {
        internal void ShouldHaveStatus(string status, int? versionNo = null)
        {
            Assert.Equal(status, appointment.Status);

            if (versionNo is not null)
            {
                Assert.Equal(versionNo.Value, appointment.VersionNo);
            }
        }

        [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Global")]
        internal void ShouldBeConvertedFrom(Guid bookingRequestId)
        {
            Assert.Equal(bookingRequestId, appointment.BookingRequestId);
            appointment.ShouldHaveStatus("Confirmed", versionNo: 1);
            Assert.Single((IEnumerable)appointment.Items);
        }
    }
}
