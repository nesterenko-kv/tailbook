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
}
