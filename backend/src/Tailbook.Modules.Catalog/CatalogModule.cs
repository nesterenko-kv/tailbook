using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Infrastructure.Persistence.Configurations;
using Tailbook.Modules.Catalog.Infrastructure.Services;

namespace Tailbook.Modules.Catalog;

public sealed class CatalogModule : IModuleDefinition
{
    public string ModuleCode => "catalog";

    public void ConfigurePersistence()
    {
        ModelConfigurationRegistry.Register(ModuleCode, CatalogModelConfiguration.Apply);
    }

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<CatalogUseCases>();
        services.AddScoped<ICatalogReadService>(sp => sp.GetRequiredService<CatalogUseCases>());
        services.AddScoped<CatalogPricingUseCases>();
        services.AddScoped<ICatalogPricingReadService>(sp => sp.GetRequiredService<CatalogPricingUseCases>());
        services.AddScoped<ICatalogQuoteResolver, CatalogQuoteResolver>();
        services.AddScoped<ICatalogOfferReadService, CatalogOfferReadService>();
        services.AddScoped<IOfferReferenceValidationService, CatalogReferenceServices>();
        services.AddScoped<IVisitCatalogReadService, CatalogVisitReadService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
