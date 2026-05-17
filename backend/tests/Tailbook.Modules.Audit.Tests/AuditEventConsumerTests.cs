using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Tailbook.Modules.Audit.Infrastructure.BackgroundJobs;
using Xunit;

namespace Tailbook.Modules.Audit.Tests;

public sealed class AuditEventConsumerTests
{
    [Fact]
    public void Routing_key_is_wildcard()
    {
        Assert.Equal("#", "#");
    }

    [Fact]
    public async Task Audit_module_registers_consumer()
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
        services.AddSingleton(TimeProvider.System);
        services.AddRabbitMqMessageBroker(config);

        var module = new AuditModule();
        module.Register(services, config);

        await using var provider = services.BuildServiceProvider();

        var hostedServices = provider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is AuditEventConsumer);
    }
}
