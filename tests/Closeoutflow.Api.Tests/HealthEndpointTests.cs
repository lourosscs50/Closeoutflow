using System.Net;
using System.Net.Http.Json;

namespace Closeoutflow.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public HealthEndpointTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Should_Return_Ok_When_Api_Is_Running()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.NotNull(body);
        Assert.Equal("Healthy", body!.Status);
        Assert.Equal("Closeoutflow.Api", body.Service);
    }

    [Fact]
    public async Task Ready_Should_Return_Ok_When_Database_Is_Reachable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ReadyResponse>();

        Assert.NotNull(body);
        Assert.Equal("Ready", body!.Status);
        Assert.Equal("Reachable", body.Database);
    }

    private sealed record HealthResponse(
        string Status,
        string Service);

    private sealed record ReadyResponse(
        string Status,
        string Database);
}
