using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Xunit;

namespace Tailbook.BuildingBlocks.Tests;

public sealed class RabbitMqOptionsTests
{
    [Fact]
    public void SectionName_is_RabbitMq()
    {
        Assert.Equal("RabbitMq", RabbitMqOptions.SectionName);
    }

    [Fact]
    public void Default_options_have_expected_values()
    {
        var options = new RabbitMqOptions();

        Assert.Equal("localhost", options.Host);
        Assert.Equal(5672, options.Port);
        Assert.Equal("/", options.VirtualHost);
        Assert.Equal("guest", options.Username);
        Assert.Equal("guest", options.Password);
        Assert.Equal("tailbook.events", options.Exchange);
        Assert.Equal(30, options.ConnectionTimeoutSeconds);
        Assert.Equal(60, options.HeartbeatSeconds);
        Assert.Equal(50, options.MaxChannels);
        Assert.False(options.Enabled);
    }

    [Fact]
    public void Binds_from_configuration_correctly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:Host"] = "rabbit.example.com",
                ["RabbitMq:Port"] = "5673",
                ["RabbitMq:Username"] = "admin",
                ["RabbitMq:Password"] = "secret",
                ["RabbitMq:Enabled"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<RabbitMqOptions>()
            .Bind(config.GetSection(RabbitMqOptions.SectionName))
            .ValidateOnStart();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

        Assert.Equal("rabbit.example.com", options.Host);
        Assert.Equal(5673, options.Port);
        Assert.Equal("admin", options.Username);
        Assert.Equal("secret", options.Password);
        Assert.True(options.Enabled);
    }

    [Fact]
    public void MessagingRegistration_registers_services_correctly()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:Enabled"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMqMessageBroker(config);

        using var provider = services.BuildServiceProvider();

        var broker = provider.GetRequiredService<IMessageBroker>();
        Assert.IsType<NoOpMessageBroker>(broker);
    }

    [Fact]
    public async Task MessagingRegistration_registers_RabbitMq_when_enabled()
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

        await using var provider = services.BuildServiceProvider();

        var factory = provider.GetService<RabbitMqConnectionFactory>();
        Assert.NotNull(factory);

        var broker = provider.GetRequiredService<IMessageBroker>();
        Assert.IsType<RabbitMqMessageBroker>(broker);
    }
}
