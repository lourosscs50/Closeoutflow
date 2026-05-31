using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Closeoutflow.Api.Tests;

public sealed class CloseoutsEndpointListContractTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public CloseoutsEndpointListContractTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCloseouts_Should_Return_Created_Closeout_When_Closeout_Exists()
    {
        var client = _factory.CreateClient();

        var (jobId, closeoutRecordId) = await client.CreateClosedOutJobAsync(
            "Replace hallway light fixture",
            "Hallway light fixture replaced and tested.",
            "photo://hallway-light-fixture");

        var listResponse = await client.GetAsync("/closeouts");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Array, listJson.ValueKind);
        Assert.True(listJson.GetArrayLength() >= 1);

        var matchingCloseout = listJson
            .EnumerateArray()
            .SingleOrDefault(closeout =>
                closeout.TryGetProperty("closeoutRecordId", out var listedCloseoutId)
                && listedCloseoutId.GetString() == closeoutRecordId.ToString());

        Assert.Equal(JsonValueKind.Object, matchingCloseout.ValueKind);

        Assert.True(matchingCloseout.TryGetProperty("jobId", out var listedJobId));
        Assert.Equal(jobId.ToString(), listedJobId.GetString());

        Assert.True(matchingCloseout.TryGetProperty("summary", out var summary));
        Assert.Equal("Hallway light fixture replaced and tested.", summary.GetString());
    }
}

public sealed class CloseoutsEndpointGetByIdContractTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public CloseoutsEndpointGetByIdContractTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCloseoutById_Should_Return_Created_Closeout_When_Closeout_Exists()
    {
        var client = _factory.CreateClient();

        var (jobId, closeoutRecordId) = await client.CreateClosedOutJobAsync(
            "Repair cabinet hinge",
            "Cabinet hinge repaired and verified.",
            "photo://cabinet-hinge-repaired");

        var getResponse = await client.GetAsync($"/closeouts/{closeoutRecordId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetchedCloseoutJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Object, fetchedCloseoutJson.ValueKind);

        Assert.True(fetchedCloseoutJson.TryGetProperty("closeoutRecordId", out var fetchedCloseoutRecordId));
        Assert.Equal(closeoutRecordId.ToString(), fetchedCloseoutRecordId.GetString());

        Assert.True(fetchedCloseoutJson.TryGetProperty("jobId", out var fetchedJobId));
        Assert.Equal(jobId.ToString(), fetchedJobId.GetString());

        Assert.True(fetchedCloseoutJson.TryGetProperty("summary", out var summary));
        Assert.Equal("Cabinet hinge repaired and verified.", summary.GetString());

        Assert.True(fetchedCloseoutJson.TryGetProperty("proofItems", out var proofItems));
        Assert.Equal(JsonValueKind.Array, proofItems.ValueKind);
        Assert.True(proofItems.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetCloseoutById_Should_Return_NotFound_When_Closeout_Does_Not_Exist()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/closeouts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
