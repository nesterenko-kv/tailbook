using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Notifications.Application;
using Tailbook.Modules.Notifications.Infrastructure;

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
            .Bind(configuration.GetSection("Notifications"));
        services.AddScoped<NotificationQueries>();
        services.AddScoped<INotificationSink, LocalFileNotificationSink>();
        services.AddScoped<IDataSeeder, NotificationTemplateSeeder>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
