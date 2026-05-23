using System.Net;

namespace Closeoutflow.Api.Tests;

public sealed class ApiReadinessTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public ApiReadinessTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnknownRoute_Should_Return_NotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/route-that-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
