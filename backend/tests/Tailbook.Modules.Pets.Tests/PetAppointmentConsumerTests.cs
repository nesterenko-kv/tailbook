using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.Modules.Pets.Infrastructure.BackgroundJobs;
using Xunit;

namespace Tailbook.Modules.Pets.Tests;

public sealed class PetAppointmentConsumerTests
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
    public async Task Pets_module_registers_consumer()
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

        var module = new PetsModule();
        module.Register(services, config);

        await using var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is PetAppointmentConsumer);
    }
}
