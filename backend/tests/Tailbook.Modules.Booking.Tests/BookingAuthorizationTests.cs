using System.Net;
using Tailbook.Api.Tests.Factories;
using Xunit;

namespace Tailbook.Modules.Booking.Tests;

public sealed class BookingAuthorizationTests(RealDbWebApplicationFactory factory)
    : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Groomer_cannot_access_admin_booking_requests()
    {
        await factory.SeedUserAsync("groomer.booking@test.local", "Booking Groomer", "Groomer123!", "groomer");
        var token = await factory.LoginAsAsync("groomer.booking@test.local", "Groomer123!");

        using var client = factory.CreateClient();
        RealDbWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/booking-requests");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
