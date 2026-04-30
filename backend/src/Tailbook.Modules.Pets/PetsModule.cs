using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Pets;

public sealed class PetsModule : IModuleDefinition
{
    public string ModuleCode => "pets";

    public void ConfigurePersistence()
    {
        ModelConfigurationRegistry.Register(ModuleCode, PetsModelConfiguration.Apply);
    }

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<PetsQueries>();
        services.AddScoped<ClientPortalPetsQueries>();
        services.AddScoped<IPetReferenceValidationService, PetReferenceServices>();
        services.AddScoped<IPetReadModelService, PetReferenceServices>();
        services.AddScoped<IPetSummaryReadService, PetReferenceServices>();
        services.AddScoped<IPetOperationalReadService, PetReferenceServices>();
        services.AddScoped<IPetQuoteProfileService, PetReferenceServices>();
        services.AddScoped<IPetTaxonomyValidationService, PetReferenceServices>();
        services.AddSingleton<IPetPhotoStorage, LocalFilesystemPetPhotoStorage>();
        services.AddScoped<IDataSeeder, PetsCatalogSeeder>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
