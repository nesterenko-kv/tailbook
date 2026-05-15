using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Notifications.Infrastructure.BackgroundJobs;
using Tailbook.Modules.Notifications.Infrastructure.Options;
using Tailbook.Modules.Notifications.Infrastructure.Seeding;
using Tailbook.Modules.Notifications.Infrastructure.Services;

namespace Tailbook.Modules.Notifications;

public sealed class NotificationsModule : IModuleDefinition
{
    public string ModuleCode => "notifications";

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<NotificationsOptions>()
            .Bind(configuration.GetSection(NotificationsOptions.SectionName))
            .Validate(x => NotificationsOptions.IsSupportedProvider(x.Provider), "Notifications:Provider must be LocalFile or Smtp.")
            .Validate(x => x.BackgroundPollIntervalSeconds >= 5, "Notifications:BackgroundPollIntervalSeconds must be at least 5 seconds.")
            .Validate(x => x.MaxDeliveryAttempts >= 1, "Notifications:MaxDeliveryAttempts must be at least 1.")
            .Validate(x => x.RetryBaseDelaySeconds >= 1, "Notifications:RetryBaseDelaySeconds must be at least 1 second.")
            .Validate(x => x.RetryMaxDelaySeconds >= x.RetryBaseDelaySeconds, "Notifications:RetryMaxDelaySeconds must be greater than or equal to RetryBaseDelaySeconds.")
            .Validate(x => !NotificationsOptions.IsLocalFileProvider(x.Provider) || !string.IsNullOrWhiteSpace(x.LocalFilePath), "Notifications:LocalFilePath is required for the LocalFile provider.")
            .Validate(x => !NotificationsOptions.IsSmtpProvider(x.Provider) || !string.IsNullOrWhiteSpace(x.SmtpHost), "Notifications:SmtpHost is required for the Smtp provider.")
            .Validate(x => !NotificationsOptions.IsSmtpProvider(x.Provider) || x.SmtpPort is >= 1 and <= 65535, "Notifications:SmtpPort must be between 1 and 65535 for the Smtp provider.")
            .Validate(x => !NotificationsOptions.IsSmtpProvider(x.Provider) || !string.IsNullOrWhiteSpace(x.SmtpFromEmail), "Notifications:SmtpFromEmail is required for the Smtp provider.")
            .Validate(x => !NotificationsOptions.IsSmtpProvider(x.Provider) || x.SmtpTimeoutSeconds >= 1, "Notifications:SmtpTimeoutSeconds must be at least 1 second for the Smtp provider.")
            .ValidateOnStart();
        services.AddScoped<NotificationUseCases>();
        services.AddScoped<INotificationReadService, NotificationUseCases>();
        services.AddScoped<LocalFileNotificationSink>();
        services.AddScoped<SmtpNotificationSink>();
        services.AddScoped<INotificationSink>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<NotificationsOptions>>().Value;
            if (NotificationsOptions.IsSmtpProvider(options.Provider))
            {
                return serviceProvider.GetRequiredService<SmtpNotificationSink>();
            }

            return serviceProvider.GetRequiredService<LocalFileNotificationSink>();
        });
        services.AddHostedService<OutboxProcessorBackgroundService>();
        services.AddScoped<IDataSeeder, NotificationTemplateSeeder>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
