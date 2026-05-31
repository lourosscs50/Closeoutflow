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

public sealed class JobsEndpointGetByIdContractTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public JobsEndpointGetByIdContractTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetJobById_Should_Return_Created_Job_When_Job_Exists()
    {
        var client = _factory.CreateClient();

        var createRequest = new
        {
            title = "Install shutoff valve"
        };

        var createResponse = await client.PostAsJsonAsync("/jobs", createRequest);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createdJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(createdJson.TryGetProperty("jobId", out var createdJobId));
        Assert.True(Guid.TryParse(createdJobId.GetString(), out var jobId));

        var getResponse = await client.GetAsync($"/jobs/{jobId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetchedJson = await getResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Object, fetchedJson.ValueKind);

        Assert.True(fetchedJson.TryGetProperty("jobId", out var fetchedJobId));
        Assert.Equal(jobId.ToString(), fetchedJobId.GetString());

        Assert.True(fetchedJson.TryGetProperty("title", out var title));
        Assert.Equal("Install shutoff valve", title.GetString());

        Assert.True(fetchedJson.TryGetProperty("status", out var status));
        Assert.False(string.IsNullOrWhiteSpace(status.GetString()));
    }

    [Fact]
    public async Task GetJobById_Should_Return_NotFound_When_Job_Does_Not_Exist()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/jobs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class JobsEndpointListContractTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public JobsEndpointListContractTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetJobs_Should_Return_Created_Job_When_Job_Exists()
    {
        var client = _factory.CreateClient();

        var createRequest = new
        {
            title = "Repair drywall"
        };

        var createResponse = await client.PostAsJsonAsync("/jobs", createRequest);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createdJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(createdJson.TryGetProperty("jobId", out var createdJobId));
        Assert.True(Guid.TryParse(createdJobId.GetString(), out var jobId));

        var listResponse = await client.GetAsync("/jobs");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Array, listJson.ValueKind);
        Assert.True(listJson.GetArrayLength() >= 1);

        var matchingJob = listJson
            .EnumerateArray()
            .SingleOrDefault(job =>
                job.TryGetProperty("jobId", out var listedJobId)
                && listedJobId.GetString() == jobId.ToString());

        Assert.Equal(JsonValueKind.Object, matchingJob.ValueKind);

        Assert.True(matchingJob.TryGetProperty("title", out var title));
        Assert.Equal("Repair drywall", title.GetString());

        Assert.True(matchingJob.TryGetProperty("status", out var status));
        Assert.False(string.IsNullOrWhiteSpace(status.GetString()));

        Assert.True(matchingJob.TryGetProperty("createdAtUtc", out var createdAtUtc));
        Assert.Equal(JsonValueKind.String, createdAtUtc.ValueKind);
    }
}

public sealed class JobsEndpointStartContractTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public JobsEndpointStartContractTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartJob_Should_Return_Ok_With_Status_Response_When_Job_Exists()
    {
        var client = _factory.CreateClient();

        var createRequest = new
        {
            title = "Paint trim"
        };

        var createResponse = await client.PostAsJsonAsync("/jobs", createRequest);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createdJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(createdJson.TryGetProperty("jobId", out var createdJobId));
        Assert.True(Guid.TryParse(createdJobId.GetString(), out var jobId));

        var startResponse = await client.PostAsync($"/jobs/{jobId}/start", content: null);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var startedJson = await startResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Object, startedJson.ValueKind);

        Assert.True(startedJson.TryGetProperty("jobId", out var startedJobId));
        Assert.Equal(jobId.ToString(), startedJobId.GetString());

        Assert.True(startedJson.TryGetProperty("status", out var status));
        Assert.False(string.IsNullOrWhiteSpace(status.GetString()));
    }

    [Fact]
    public async Task StartJob_Should_Return_NotFound_When_Job_Does_Not_Exist()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/jobs/{Guid.NewGuid()}/start", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class JobsEndpointMarkPendingCloseoutContractTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public JobsEndpointMarkPendingCloseoutContractTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MarkPendingCloseout_Should_Return_Ok_With_Status_Response_When_Job_Exists()
    {
        var client = _factory.CreateClient();

        var createRequest = new
        {
            title = "Replace outlet cover"
        };

        var createResponse = await client.PostAsJsonAsync("/jobs", createRequest);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createdJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(createdJson.TryGetProperty("jobId", out var createdJobId));
        Assert.True(Guid.TryParse(createdJobId.GetString(), out var jobId));

        var startResponse = await client.PostAsync($"/jobs/{jobId}/start", content: null);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var pendingResponse = await client.PostAsync($"/jobs/{jobId}/mark-pending-closeout", content: null);

        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);

        var pendingJson = await pendingResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Object, pendingJson.ValueKind);

        Assert.True(pendingJson.TryGetProperty("jobId", out var pendingJobId));
        Assert.Equal(jobId.ToString(), pendingJobId.GetString());

        Assert.True(pendingJson.TryGetProperty("status", out var status));
        Assert.False(string.IsNullOrWhiteSpace(status.GetString()));
    }

    [Fact]
    public async Task MarkPendingCloseout_Should_Return_NotFound_When_Job_Does_Not_Exist()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/jobs/{Guid.NewGuid()}/mark-pending-closeout", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class JobsEndpointCloseoutContractTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public JobsEndpointCloseoutContractTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CloseoutJob_Should_Return_Ok_With_Closeout_Response_When_Job_Is_Pending_Closeout()
    {
        var client = _factory.CreateClient();

        var createRequest = new
        {
            title = "Install smoke detector"
        };

        var createResponse = await client.PostAsJsonAsync("/jobs", createRequest);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createdJson = await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(createdJson.TryGetProperty("jobId", out var createdJobId));
        Assert.True(Guid.TryParse(createdJobId.GetString(), out var jobId));

        var startResponse = await client.PostAsync($"/jobs/{jobId}/start", content: null);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var pendingResponse = await client.PostAsync($"/jobs/{jobId}/mark-pending-closeout", content: null);

        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);

        var closeoutRequest = new
        {
            summary = "Smoke detector installed and tested.",
            proofItems = new[]
            {
                new
                {
                    type = 1,
                    value = "photo://smoke-detector-installed"
                }
            }
        };

        var closeoutResponse = await client.PostAsJsonAsync($"/jobs/{jobId}/closeout", closeoutRequest);

        Assert.Equal(HttpStatusCode.OK, closeoutResponse.StatusCode);

        var closeoutJson = await closeoutResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(JsonValueKind.Object, closeoutJson.ValueKind);

        Assert.True(closeoutJson.TryGetProperty("closeoutRecordId", out var closeoutRecordId));
        Assert.True(Guid.TryParse(closeoutRecordId.GetString(), out _));

        Assert.True(closeoutJson.TryGetProperty("jobId", out var closeoutJobId));
        Assert.Equal(jobId.ToString(), closeoutJobId.GetString());

        Assert.True(closeoutJson.TryGetProperty("jobStatus", out var jobStatus));
        Assert.False(string.IsNullOrWhiteSpace(jobStatus.GetString()));
    }

    [Fact]
    public async Task CloseoutJob_Should_Return_BadRequest_When_Job_Does_Not_Exist()
    {
        var client = _factory.CreateClient();

        var closeoutRequest = new
        {
            summary = "Missing job closeout.",
            proofItems = Array.Empty<object>()
        };

        var response = await client.PostAsJsonAsync($"/jobs/{Guid.NewGuid()}/closeout", closeoutRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

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

        var createJobRequest = new
        {
            title = "Replace hallway light fixture"
        };

        var createJobResponse = await client.PostAsJsonAsync("/jobs", createJobRequest);

        Assert.Equal(HttpStatusCode.OK, createJobResponse.StatusCode);

        var createdJobJson = await createJobResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(createdJobJson.TryGetProperty("jobId", out var createdJobId));
        Assert.True(Guid.TryParse(createdJobId.GetString(), out var jobId));

        var startResponse = await client.PostAsync($"/jobs/{jobId}/start", content: null);

        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var pendingResponse = await client.PostAsync($"/jobs/{jobId}/mark-pending-closeout", content: null);

        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);

        var closeoutRequest = new
        {
            summary = "Hallway light fixture replaced and tested.",
            proofItems = new[]
            {
                new
                {
                    type = 1,
                    value = "photo://hallway-light-fixture"
                }
            }
        };

        var closeoutResponse = await client.PostAsJsonAsync($"/jobs/{jobId}/closeout", closeoutRequest);

        Assert.Equal(HttpStatusCode.OK, closeoutResponse.StatusCode);

        var createdCloseoutJson = await closeoutResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(createdCloseoutJson.TryGetProperty("closeoutRecordId", out var createdCloseoutRecordId));
        Assert.True(Guid.TryParse(createdCloseoutRecordId.GetString(), out var closeoutRecordId));

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
