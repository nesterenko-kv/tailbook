using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Notifications.Infrastructure.BackgroundJobs;
using Tailbook.Modules.Notifications.Infrastructure.Options;
using Tailbook.Modules.Notifications.Infrastructure.Persistence.Configurations;
using Tailbook.Modules.Notifications.Infrastructure.Seeding;
using Tailbook.Modules.Notifications.Infrastructure.Services;

namespace Tailbook.Modules.Notifications;

public sealed class NotificationsModule : IModuleDefinition
{
    public string ModuleCode => "notifications";

    public void ConfigurePersistence()
    {
        ModelConfigurationRegistry.Register(ModuleCode, NotificationsModelConfiguration.Apply);
    }

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<NotificationsOptions>()
            .Bind(configuration.GetSection(NotificationsOptions.SectionName))
            .Validate(x => x.BackgroundPollIntervalSeconds >= 5, "Notifications:BackgroundPollIntervalSeconds must be at least 5 seconds.")
            .Validate(x => !string.IsNullOrWhiteSpace(x.LocalFilePath), "Notifications:LocalFilePath is required.")
            .ValidateOnStart();
        services.AddScoped<NotificationUseCases>();
        services.AddScoped<INotificationReadService>(sp => sp.GetRequiredService<NotificationUseCases>());
        services.AddScoped<INotificationSink, LocalFileNotificationSink>();
        services.AddHostedService<OutboxProcessorBackgroundService>();
        services.AddScoped<IDataSeeder, NotificationTemplateSeeder>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
