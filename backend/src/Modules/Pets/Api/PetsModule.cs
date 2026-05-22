using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Pets.Infrastructure.BackgroundJobs;
using Tailbook.Modules.Pets.Infrastructure.Seeding;
using Tailbook.Modules.Pets.Infrastructure.Services;

namespace Tailbook.Modules.Pets;

public sealed class PetsModule : IModuleDefinition
{
    public string ModuleCode => "pets";

    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<PetsUseCases>();
        services.AddScoped<IPetsReadService, PetsUseCases>();
        services.AddScoped<IClientPortalPetsReadService, ClientPortalPetsReadService>();
        services.AddScoped<IPetReferenceValidationService, PetReferenceServices>();
        services.AddScoped<IPetReadModelService, PetReferenceServices>();
        services.AddScoped<IPetSummaryReadService, PetReferenceServices>();
        services.AddScoped<IPetOperationalReadService, PetReferenceServices>();
        services.AddScoped<IPetQuoteProfileService, PetReferenceServices>();
        services.AddScoped<IPetTaxonomyValidationService, PetReferenceServices>();
        services.AddSingleton<IPetPhotoStorage, LocalFilesystemPetPhotoStorage>();
        services.AddScoped<IDataSeeder, PetsCatalogSeeder>();
        services.AddHostedService<PetAppointmentConsumer>();
        return services;
    }

    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
