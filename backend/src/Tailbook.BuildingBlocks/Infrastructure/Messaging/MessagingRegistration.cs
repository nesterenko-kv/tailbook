using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.BuildingBlocks.Infrastructure.Messaging;

public static class MessagingRegistration
{
    public static IServiceCollection AddRabbitMqMessageBroker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<IMessageBroker>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>();
            if (!options.Value.Enabled)
            {
                var logger = sp.GetRequiredService<ILogger<NoOpMessageBroker>>();
                return new NoOpMessageBroker(logger);
            }

            return ActivatorUtilities.CreateInstance<RabbitMqMessageBroker>(sp);
        });

        return services;
    }
}
