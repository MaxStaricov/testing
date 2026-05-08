using Npgsql;
using Microsoft.Extensions.Configuration; // ✅ Добавлено
using Microsoft.Extensions.Configuration.Json; // ✅ Требуется для .AddJsonFile()

namespace TestModule.Backend.IntegrationTests.Fixtures;

public static class DatabaseFixture
{
    private const string ConfigKey = "ConnectionStrings:DefaultConnection";

    public static string ConnectionString { get; } = ResolveConnectionString();

    private static string ResolveConnectionString()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build();

        return config[ConfigKey] ?? throw new Exception("Connection string not found in appsettings.Test.json");
    }

    public static async Task SeedAsync()
    {
        var seedPath = Path.Combine(AppContext.BaseDirectory, "TestData", "seed.sql");
        var sql = await File.ReadAllTextAsync(seedPath);

        int retries = 3;
        while (retries > 0)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
                return;
            }
            catch (PostgresException)
            {
                retries--;
                if (retries == 0) throw;
                await Task.Delay(2000);
            }
        }
    }

    public static Task CleanSeedAsync()
    {
        return Task.CompletedTask;
    }
}
