using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.Modules.VisitOperations.Infrastructure.BackgroundJobs;
using Xunit;

namespace Tailbook.Modules.VisitOperations.Tests;

public sealed class VisitCancellationConsumerTests
{
    [Fact]
    public void Routing_key_matches_appointment_cancelled()
    {
        const string routingKey = "booking.appointment-cancelled";
        Assert.Equal("booking.appointment-cancelled", routingKey);
    }

    [Fact]
    public void Does_not_match_other_appointment_events()
    {
        var cancellationKey = "booking.appointment-cancelled";

        Assert.NotEqual("booking.appointment-created", cancellationKey);
        Assert.NotEqual("booking.appointment-rescheduled", cancellationKey);
        Assert.NotEqual("booking.booking-requested", cancellationKey);
    }

    [Fact]
    public async Task VisitOperations_module_registers_consumer()
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

        var module = new VisitOperationsModule();
        module.Register(services, config);

        await using var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is VisitCancellationConsumer);
    }
}
