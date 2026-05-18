using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;

public static class IntegrationOutboxPublisherRegistration
{
    public static IServiceCollection AddIntegrationOutboxPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<IntegrationOutboxPublisherOptions>()
            .Bind(configuration.GetSection(IntegrationOutboxPublisherOptions.SectionName))
            .Validate(
                x => x.PollIntervalSeconds >= 5,
                "IntegrationOutbox:PollIntervalSeconds must be at least 5 seconds.")
            .ValidateOnStart();

        services.AddHostedService<IntegrationOutboxPublisherBackgroundService>();
        return services;
    }
}
