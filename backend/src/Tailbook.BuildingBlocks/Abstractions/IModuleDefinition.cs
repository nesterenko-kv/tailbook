using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tailbook.BuildingBlocks.Abstractions;

public interface IModuleDefinition
{
    string ModuleCode { get; }

    IServiceCollection Register(IServiceCollection services, IConfiguration configuration);

    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
