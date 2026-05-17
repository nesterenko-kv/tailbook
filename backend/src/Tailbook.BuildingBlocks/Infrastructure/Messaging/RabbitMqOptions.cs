using System.ComponentModel.DataAnnotations;

namespace Tailbook.BuildingBlocks.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "tailbook.events";
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int HeartbeatSeconds { get; set; } = 60;
    public int MaxChannels { get; set; } = 50;
    public bool Enabled { get; set; }
}
