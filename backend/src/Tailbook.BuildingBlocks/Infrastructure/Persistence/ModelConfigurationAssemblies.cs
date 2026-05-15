using System.Reflection;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence;

public sealed class ModelConfigurationAssemblies
{
    public ModelConfigurationAssemblies(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var values = new List<Assembly>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            if (seen.Add(assembly.FullName ?? assembly.GetName().Name ?? assembly.Location))
            {
                values.Add(assembly);
            }
        }

        Assemblies = values;
    }

    public IReadOnlyList<Assembly> Assemblies { get; }
}
