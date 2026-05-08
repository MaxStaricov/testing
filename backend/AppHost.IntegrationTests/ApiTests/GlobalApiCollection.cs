using Xunit;
using TestModule.Backend.IntegrationTests.Fixtures;

namespace TestModule.Backend.IntegrationTests.ApiTests;

[CollectionDefinition("ApiCollection")]
public class GlobalApiCollection : ICollectionFixture<ApiFixture>
{
}
