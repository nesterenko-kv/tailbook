using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Customer.Infrastructure.Persistence.Configurations;
using Tailbook.Modules.Customer.Infrastructure.Services;

namespace Tailbook.Modules.Customer;

public sealed class CustomerModule : IModuleDefinition
{
    public string ModuleCode => "customer";

    public void ConfigurePersistence()
    {
        ModelConfigurationRegistry.Register(ModuleCode, CustomerModelConfiguration.Apply);
    }

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<CustomerUseCases>();
        services.AddScoped<ICustomerReadService>(sp => sp.GetRequiredService<CustomerUseCases>());
        services.AddScoped<ClientPortalCustomerUseCases>();
        services.AddScoped<IClientPortalCustomerReadService>(sp => sp.GetRequiredService<ClientPortalCustomerUseCases>());
        services.AddScoped<IClientReferenceValidationService, CustomerReferenceServices>();
        services.AddScoped<IContactReferenceValidationService, CustomerReferenceServices>();
        services.AddScoped<IPetContactReadModelService, CustomerReferenceServices>();
        services.AddScoped<IClientOnboardingService, CustomerReferenceServices>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
