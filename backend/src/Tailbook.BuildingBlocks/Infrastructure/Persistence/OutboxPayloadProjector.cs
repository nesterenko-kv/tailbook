using System.Reflection;
using System.Text.Json;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence;

internal static class OutboxPayloadProjector
{
    private static readonly HashSet<string> MetadataPropertyNames =
    [
        nameof(IDomainEvent.EventId),
        nameof(IDomainEvent.OccurredAt),
        nameof(IDomainEvent.EventType),
        nameof(IDomainEvent.ModuleCode)
    ];

    public static object Project(IDomainEvent domainEvent)
    {
        var payload = new Dictionary<string, object?>();
        var properties = domainEvent.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in properties)
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0 || MetadataPropertyNames.Contains(property.Name))
            {
                continue;
            }

            var key = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            payload[key] = property.GetValue(domainEvent);
        }

        return payload;
    }
}
