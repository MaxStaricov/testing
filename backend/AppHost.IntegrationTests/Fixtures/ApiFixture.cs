using Xunit;
using System.Net.Http;

namespace TestModule.Backend.IntegrationTests.Fixtures;

public class ApiFixture : IAsyncLifetime
{
    private HttpClient? _client;
    public HttpClient Client => _client ?? throw new InvalidOperationException("Client not initialized");

    public const string BaseUrl = "http://localhost:5117";

    public async Task InitializeAsync()
    {
        await DatabaseFixture.SeedAsync();
        _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        await WaitForApiReadyAsync();
    }

    private async Task WaitForApiReadyAsync()
    {
        int maxRetries = 10;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await Client.GetAsync("/api/products");
                if (response.IsSuccessStatusCode) return;
            }
            catch
            {
            }
            await Task.Delay(2000);
        }
        throw new Exception($"API at {BaseUrl} is still not ready after {maxRetries} retries.");
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        return Task.CompletedTask;
    }
}