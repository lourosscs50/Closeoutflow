using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Closeoutflow.Api.Tests;

public sealed class JobsEndpointEmptyStateContractTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public JobsEndpointEmptyStateContractTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetJobs_Should_Return_Ok_With_Empty_Array_When_No_Jobs_Exist()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        Assert.Equal(0, json.GetArrayLength());
    }
}

public sealed class JobsEndpointCreateContractTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public JobsEndpointCreateContractTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateJob_Should_Return_Ok_With_Job_Response_When_Title_Is_Valid()
    {
        var client = _factory.CreateClient();

        var request = new
        {
            title = "Replace water heater"
        };

        var response = await client.PostAsJsonAsync("/jobs", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Object, json.ValueKind);

        Assert.True(json.TryGetProperty("jobId", out var jobId));
        Assert.True(Guid.TryParse(jobId.GetString(), out _));

        Assert.True(json.TryGetProperty("title", out var title));
        Assert.Equal("Replace water heater", title.GetString());

        Assert.True(json.TryGetProperty("status", out var status));
        Assert.False(string.IsNullOrWhiteSpace(status.GetString()));
    }
}

public sealed class JobsEndpointInvalidCreateContractTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public JobsEndpointInvalidCreateContractTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateJob_Should_Return_BadRequest_When_Title_Is_Blank()
    {
        var client = _factory.CreateClient();

        var request = new
        {
            title = "   "
        };

        var response = await client.PostAsJsonAsync("/jobs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Object, json.ValueKind);
        Assert.True(json.TryGetProperty("error", out var error));
        Assert.False(string.IsNullOrWhiteSpace(error.GetString()));

        Assert.True(json.TryGetProperty("message", out var message));
        Assert.False(string.IsNullOrWhiteSpace(message.GetString()));
    }
}
