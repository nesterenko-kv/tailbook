using Microsoft.EntityFrameworkCore;

namespace Tailbook.BuildingBlocks.Infrastructure.Persistence;

public static class ModelConfigurationRegistry
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, Action<ModelBuilder>> Registrations = new(StringComparer.OrdinalIgnoreCase);

    public static void Register(string key, Action<ModelBuilder> registration)
    {
        lock (Sync)
        {
            Registrations[key] = registration;
        }
    }

    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        lock (Sync)
        {
            foreach (var registration in Registrations.Values)
            {
                registration(modelBuilder);
            }
        }
    }
}
