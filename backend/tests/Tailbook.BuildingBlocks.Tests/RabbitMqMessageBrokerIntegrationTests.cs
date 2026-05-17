using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Testcontainers.RabbitMq;
using Xunit;

namespace Tailbook.BuildingBlocks.Tests;

public sealed class RabbitMqMessageBrokerIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:4.1-management-alpine")
        .WithCleanUp(true)
        .Build();

    private RabbitMqConnectionFactory _connectionFactory = null!;
    private IMessageBroker _broker = null!;
    private RabbitMqOptions _options = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _options = new RabbitMqOptions
        {
            Enabled = true,
            Host = _container.Hostname,
            Port = _container.GetMappedPublicPort(5672),
            Username = "guest",
            Password = "guest",
            Exchange = "tailbook.test.events"
        };

        _connectionFactory = new RabbitMqConnectionFactory(
            Options.Create(_options),
            NullLogger<RabbitMqConnectionFactory>.Instance);

        _broker = new RabbitMqMessageBroker(
            _connectionFactory,
            Options.Create(_options),
            NullLogger<RabbitMqMessageBroker>.Instance);
    }

    public async Task DisposeAsync()
    {
        await _connectionFactory.DisposeAsync();
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Publish_and_consume_message_roundtrip()
    {
        var exchange = _options.Exchange;
        var routingKey = "test.roundtrip";
        var receivedTcs = new TaskCompletionSource<string>();
        var queue = "test-queue-roundtrip";

        var channel = await _connectionFactory.CreateChannelAsync();
        await channel.QueueDeclareAsync(queue, durable: false, exclusive: true, autoDelete: true);
        await channel.QueueBindAsync(queue, exchange, routingKey);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            var body = Encoding.UTF8.GetString(args.Body.Span);
            receivedTcs.TrySetResult(body);
            await channel.BasicAckAsync(args.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer);

        var payload = new { message = "hello", value = 42 };
        await _broker.PublishAsync(exchange, routingKey, payload, messageId: "test-1");

        var completed = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.NotNull(completed);

        using var document = JsonDocument.Parse(completed);
        var root = document.RootElement;
        Assert.Equal("hello", root.GetProperty("message").GetString());
        Assert.Equal(42, root.GetProperty("value").GetInt32());
    }

    [Fact]
    public async Task Publish_with_multiple_routing_keys_routes_correctly()
    {
        var exchange = _options.Exchange;
        var queue1 = "q-a";
        var queue2 = "q-b";
        var received1 = new List<string>();
        var received2 = new List<string>();

        var channel = await _connectionFactory.CreateChannelAsync();
        await channel.QueueDeclareAsync(queue1, durable: false, exclusive: true, autoDelete: true);
        await channel.QueueDeclareAsync(queue2, durable: false, exclusive: true, autoDelete: true);
        await channel.QueueBindAsync(queue1, exchange, "key.a");
        await channel.QueueBindAsync(queue2, exchange, "key.b");

        var consumer1 = new AsyncEventingBasicConsumer(channel);
        consumer1.ReceivedAsync += async (_, args) =>
        {
            received1.Add(args.RoutingKey);
            await channel.BasicAckAsync(args.DeliveryTag, false);
        };

        var consumer2 = new AsyncEventingBasicConsumer(channel);
        consumer2.ReceivedAsync += async (_, args) =>
        {
            received2.Add(args.RoutingKey);
            await channel.BasicAckAsync(args.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(queue1, autoAck: false, consumer: consumer1);
        await channel.BasicConsumeAsync(queue2, autoAck: false, consumer: consumer2);

        await _broker.PublishAsync(exchange, "key.a", new { }, messageId: "m1");
        await _broker.PublishAsync(exchange, "key.b", new { }, messageId: "m2");

        await Task.Delay(1000);

        var keyA = Assert.Single(received1);
        Assert.Equal("key.a", keyA);
        var keyB = Assert.Single(received2);
        Assert.Equal("key.b", keyB);
    }

    [Fact]
    public async Task Publish_sets_message_properties()
    {
        var exchange = _options.Exchange;
        var routingKey = "test.props";
        var receivedTcs = new TaskCompletionSource<(string messageId, string contentType, RabbitMQ.Client.DeliveryModes deliveryMode)>();
        var queue = "q-props";

        var channel = await _connectionFactory.CreateChannelAsync();
        await channel.QueueDeclareAsync(queue, durable: false, exclusive: true, autoDelete: true);
        await channel.QueueBindAsync(queue, exchange, routingKey);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            receivedTcs.TrySetResult((
                args.BasicProperties.MessageId ?? "",
                args.BasicProperties.ContentType ?? "",
                (RabbitMQ.Client.DeliveryModes)args.BasicProperties.DeliveryMode
            ));
            await channel.BasicAckAsync(args.DeliveryTag, false);
        };

        await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer);

        await _broker.PublishAsync(exchange, routingKey, new { }, messageId: "props-test");

        var (messageId, contentType, deliveryMode) = await receivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal("props-test", messageId);
        Assert.Equal("application/json", contentType);
        Assert.Equal(RabbitMQ.Client.DeliveryModes.Persistent, deliveryMode);
    }
}
