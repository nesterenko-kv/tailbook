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
        services.AddScoped<CatalogQueries>();
        services.AddScoped<ICatalogQueries>(sp => sp.GetRequiredService<CatalogQueries>());
        services.AddScoped<CatalogPricingQueries>();
        services.AddScoped<ICatalogPricingQueries>(sp => sp.GetRequiredService<CatalogPricingQueries>());
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
