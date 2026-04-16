using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Customer.Application;
using Tailbook.Modules.Customer.Infrastructure;

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
        services.AddScoped<CustomerQueries>();
        services.AddScoped<ICustomerAccessPolicy, CustomerAccessPolicy>();
        services.AddScoped<IClientReferenceValidationService, CustomerReferenceServices>();
        services.AddScoped<IPetContactReadModelService, CustomerReferenceServices>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
