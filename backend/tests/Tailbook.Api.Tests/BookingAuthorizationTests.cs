using System.Net;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class BookingAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BookingAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Groomer_cannot_access_admin_booking_requests()
    {
        await _factory.SeedUserAsync("groomer.booking@test.local", "Booking Groomer", "Groomer123!", "groomer");
        var token = await _factory.LoginAsAsync("groomer.booking@test.local", "Groomer123!");

        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var response = await client.GetAsync("/api/admin/booking-requests");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
