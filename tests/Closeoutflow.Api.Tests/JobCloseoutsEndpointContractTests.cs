using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Closeoutflow.Api.Tests;

public sealed class JobCloseoutsEndpointListContractTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public JobCloseoutsEndpointListContractTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetJobCloseouts_Should_Return_Closeouts_For_Existing_Job()
    {
        var client = _factory.CreateClient();

        var (jobId, closeoutRecordId) = await client.CreateClosedOutJobAsync(
            "Repair closet door",
            "Closet door repaired and verified.",
            "photo://closet-door-repaired");

        var listResponse = await client.GetAsync($"/jobs/{jobId}/closeouts");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Array, listJson.ValueKind);
        Assert.True(listJson.GetArrayLength() >= 1);

        var matchingCloseout = listJson
            .EnumerateArray()
            .SingleOrDefault(closeout =>
                closeout.TryGetProperty("closeoutRecordId", out var listedCloseoutRecordId)
                && listedCloseoutRecordId.GetString() == closeoutRecordId.ToString());

        Assert.Equal(JsonValueKind.Object, matchingCloseout.ValueKind);

        Assert.True(matchingCloseout.TryGetProperty("jobId", out var listedJobId));
        Assert.Equal(jobId.ToString(), listedJobId.GetString());

        Assert.True(matchingCloseout.TryGetProperty("summary", out var summary));
        Assert.Equal("Closet door repaired and verified.", summary.GetString());
    }

    [Fact]
    public async Task GetJobCloseouts_Should_Return_NotFound_When_Job_Does_Not_Exist()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/jobs/{Guid.NewGuid()}/closeouts");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
