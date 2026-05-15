using System.Data.Common;
using System.Globalization;
using Npgsql;

namespace Tailbook.Api.Host.Infrastructure;

public sealed class DatabaseConnectionOptions
{
    public const string MainConnectionStringName = "Main";

    public string? Main { get; set; }

    public static bool HasValidMainConnectionString(DatabaseConnectionOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Main))
        {
            return false;
        }

        try
        {
            var rawBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = options.Main
            };
            var npgsqlBuilder = new NpgsqlConnectionStringBuilder(options.Main);

            return HasConfiguredValue(rawBuilder, "Host", "Server")
                   && HasConfiguredValue(rawBuilder, "Database", "Initial Catalog")
                   && HasConfiguredValue(rawBuilder, "Username", "User ID", "UserName", "User")
                   && !string.IsNullOrWhiteSpace(npgsqlBuilder.Host)
                   && !string.IsNullOrWhiteSpace(npgsqlBuilder.Database)
                   && !string.IsNullOrWhiteSpace(npgsqlBuilder.Username);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool HasConfiguredValue(DbConnectionStringBuilder builder, params string[] keys)
    {
        foreach (var configuredKey in builder.Keys)
        {
            var key = Convert.ToString(configuredKey, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(key) || !keys.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(Convert.ToString(builder[key], CultureInfo.InvariantCulture)))
            {
                return true;
            }
        }

        return false;
    }
}
