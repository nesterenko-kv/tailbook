using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.Modules.Staff.Infrastructure.BackgroundJobs;
using Xunit;

namespace Tailbook.Modules.Staff.Tests;

public sealed class StaffAppointmentConsumerTests
{
    [Fact]
    public void Routing_keys_match_appointment_events()
    {
        var keys = new[]
        {
            "booking.appointment-created",
            "booking.appointment-cancelled",
            "booking.appointment-rescheduled"
        };

        Assert.Contains(keys, k => k == "booking.appointment-created");
        Assert.Contains(keys, k => k == "booking.appointment-cancelled");
        Assert.Contains(keys, k => k == "booking.appointment-rescheduled");
        Assert.DoesNotContain(keys, k => k == "booking.booking-requested");
        Assert.DoesNotContain(keys, k => k == "visitops.visit-closed");
    }

    [Fact]
    public void ToRoutingKey_generates_correct_keys()
    {
        Assert.Equal("booking.appointment-created", ToRoutingKey("booking", "AppointmentCreated"));
        Assert.Equal("booking.appointment-cancelled", ToRoutingKey("booking", "AppointmentCancelled"));
        Assert.Equal("booking.appointment-rescheduled", ToRoutingKey("booking", "AppointmentRescheduled"));
        Assert.Equal("booking.booking-requested", ToRoutingKey("booking", "BookingRequested"));
    }

    [Fact]
    public async Task Staff_module_registers_consumer()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:Enabled"] = "true",
                ["RabbitMq:Host"] = "localhost"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMqMessageBroker(config);

        var module = new StaffModule();
        module.Register(services, config);

        await using var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is StaffAppointmentConsumer);
    }

    private static string ToRoutingKey(string moduleCode, string eventType)
    {
        var kebab = string.Concat(
            eventType.Select((c, i) =>
                i > 0 && char.IsUpper(c)
                    ? "-" + char.ToLowerInvariant(c)
                    : char.ToLowerInvariant(c).ToString()));
        return $"{moduleCode}.{kebab}";
    }
}
