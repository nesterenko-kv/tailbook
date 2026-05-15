using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Catalog.Infrastructure.Services;

namespace Tailbook.Modules.Catalog;

public sealed class CatalogModule : IModuleDefinition
{
    public string ModuleCode => "catalog";

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICatalogReadService, CatalogReadService>();
        services.AddScoped<ICatalogPricingReadService, CatalogPricingReadService>();
        services.AddScoped<ICatalogQuoteResolver, CatalogQuoteResolver>();
        services.AddScoped<ICatalogOfferReadService, CatalogOfferReadService>();
        services.AddScoped<ICatalogOfferImportService, CatalogOfferImportService>();
        services.AddScoped<IOfferReferenceValidationService, CatalogReferenceServices>();
        services.AddScoped<IVisitCatalogReadService, CatalogVisitReadService>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
