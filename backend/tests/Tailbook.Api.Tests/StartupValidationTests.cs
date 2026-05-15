using Tailbook.Api.Host.Infrastructure;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class StartupValidationTests
{
    [Fact]
    public void Main_connection_string_accepts_valid_postgresql_shape()
    {
        Assert.True(DatabaseConnectionOptions.HasValidMainConnectionString(new DatabaseConnectionOptions
        {
            Main = "Host=localhost;Port=5432;Database=tailbook;Username=tailbook;Password=tailbook"
        }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Database=tailbook;Username=tailbook;Password=tailbook")]
    [InlineData("Host=localhost;Username=tailbook;Password=tailbook")]
    [InlineData("Host=localhost;Database=tailbook;Password=tailbook")]
    [InlineData("not a connection string")]
    public void Main_connection_string_rejects_missing_or_malformed_postgresql_shape(string connectionString)
    {
        Assert.False(DatabaseConnectionOptions.HasValidMainConnectionString(new DatabaseConnectionOptions
        {
            Main = connectionString
        }));
    }

    [Fact]
    public void Telemetry_options_accept_empty_or_valid_otlp_endpoint()
    {
        Assert.True(TelemetryOptions.HasValidOtlpEndpoint(new TelemetryOptions()));
        Assert.True(TelemetryOptions.HasValidOtlpEndpoint(new TelemetryOptions
        {
            OtlpEndpoint = "http://otel-collector:4317"
        }));
    }

    [Fact]
    public void Telemetry_options_only_export_logs_when_enabled_and_otlp_is_configured()
    {
        Assert.False(new TelemetryOptions().ShouldExportLogs);
        Assert.True(new TelemetryOptions
        {
            OtlpEndpoint = "http://otel-collector:4317"
        }.ShouldExportLogs);
        Assert.False(new TelemetryOptions
        {
            Enabled = false,
            OtlpEndpoint = "http://otel-collector:4317"
        }.ShouldExportLogs);
        Assert.False(new TelemetryOptions
        {
            ExportLogs = false,
            OtlpEndpoint = "http://otel-collector:4317"
        }.ShouldExportLogs);
        Assert.False(new TelemetryOptions
        {
            OtlpEndpoint = "not a uri"
        }.ShouldExportLogs);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Telemetry_options_reject_missing_database_pool_name(string databasePoolName)
    {
        Assert.False(TelemetryOptions.HasValidDatabasePoolName(new TelemetryOptions
        {
            DatabasePoolName = databasePoolName
        }));
    }

    [Theory]
    [InlineData("localhost:4317")]
    [InlineData("ftp://otel-collector:4317")]
    [InlineData("not a uri")]
    public void Telemetry_options_reject_malformed_otlp_endpoint(string endpoint)
    {
        Assert.False(TelemetryOptions.HasValidOtlpEndpoint(new TelemetryOptions
        {
            OtlpEndpoint = endpoint
        }));
    }
}
