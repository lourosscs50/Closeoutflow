using System.Net;
using System.Net.Http.Json;

namespace Closeoutflow.Api.Tests;

public class CloseoutWorkflowTests : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public CloseoutWorkflowTests(CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }


    [Fact]
    public async Task Create_Job_Should_Use_Requested_Title()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/jobs",
            new CreateJobRequest("  Replace rooftop unit  "));

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateJobResponse>();
        Assert.NotNull(createBody);
        Assert.NotEqual(Guid.Empty, createBody!.JobId);
        Assert.Equal("Replace rooftop unit", createBody.Title);
        Assert.Equal("New", createBody.Status);

        var jobReadBody = await ReadJobAsync(client, createBody.JobId);
        Assert.Equal("Replace rooftop unit", jobReadBody.Title);
        Assert.Equal("New", jobReadBody.Status);
    }

    [Fact]
    public async Task Create_Job_Should_Return_BadRequest_When_Title_Is_Blank()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/jobs",
            new CreateJobRequest("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(errorBody);
        Assert.Equal("Jobs.TitleRequired", errorBody!.Error);
    }

    [Fact]
    public async Task Full_Closeout_Workflow_Should_Succeed()
    {
        var client = _factory.CreateClient();

        var createBody = await CreateJobAsync(client);

        var startResponse = await client.PostAsync($"/jobs/{createBody.JobId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var pendingResponse = await client.PostAsync($"/jobs/{createBody.JobId}/mark-pending-closeout", null);
        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);

        var closeoutRequest = new CompleteJobCloseoutRequest(
            "Completed rooftop unit replacement",
            new[]
            {
                new ProofItemRequest(0, "Work completed successfully"),
                new ProofItemRequest(1, "https://example.com/photo.jpg")
            });

        var closeoutResponse = await client.PostAsJsonAsync(
            $"/jobs/{createBody.JobId}/closeout",
            closeoutRequest);

        Assert.Equal(HttpStatusCode.OK, closeoutResponse.StatusCode);

        var closeoutBody = await closeoutResponse.Content.ReadFromJsonAsync<CompleteJobCloseoutResponse>();
        Assert.NotNull(closeoutBody);
        Assert.Equal(createBody.JobId, closeoutBody!.JobId);
        Assert.Equal("Closed", closeoutBody.JobStatus);
        Assert.NotEqual(Guid.Empty, closeoutBody.CloseoutRecordId);
    }

    [Fact]
    public async Task Closeout_And_Readback_Should_Return_Closed_Job_And_Closeout_Record()
    {
        var client = _factory.CreateClient();

        var createBody = await CreateJobAsync(client);

        var startResponse = await client.PostAsync($"/jobs/{createBody.JobId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var pendingResponse = await client.PostAsync($"/jobs/{createBody.JobId}/mark-pending-closeout", null);
        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);

        var closeoutRequest = new CompleteJobCloseoutRequest(
            "Completed rooftop unit replacement",
            new[]
            {
                new ProofItemRequest(0, "Work completed successfully"),
                new ProofItemRequest(1, "https://example.com/photo.jpg")
            });

        var closeoutResponse = await client.PostAsJsonAsync(
            $"/jobs/{createBody.JobId}/closeout",
            closeoutRequest);

        Assert.Equal(HttpStatusCode.OK, closeoutResponse.StatusCode);

        var closeoutBody = await closeoutResponse.Content.ReadFromJsonAsync<CompleteJobCloseoutResponse>();
        Assert.NotNull(closeoutBody);

        var jobReadResponse = await client.GetAsync($"/jobs/{createBody.JobId}");
        Assert.Equal(HttpStatusCode.OK, jobReadResponse.StatusCode);

        var jobReadBody = await jobReadResponse.Content.ReadFromJsonAsync<JobReadResponse>();
        Assert.NotNull(jobReadBody);
        Assert.Equal(createBody.JobId, jobReadBody!.JobId);
        Assert.Equal("Closed", jobReadBody.Status);
        Assert.NotNull(jobReadBody.ClosedAtUtc);

        var closeoutReadResponse = await client.GetAsync($"/closeouts/{closeoutBody!.CloseoutRecordId}");
        Assert.Equal(HttpStatusCode.OK, closeoutReadResponse.StatusCode);

        var closeoutReadBody = await closeoutReadResponse.Content.ReadFromJsonAsync<CloseoutReadResponse>();
        Assert.NotNull(closeoutReadBody);
        Assert.Equal(closeoutBody.CloseoutRecordId, closeoutReadBody!.CloseoutRecordId);
        Assert.Equal(createBody.JobId, closeoutReadBody.JobId);
        Assert.Equal("Completed rooftop unit replacement", closeoutReadBody.Summary);
        Assert.Equal(2, closeoutReadBody.ProofItems.Length);
    }


    [Fact]
    public async Task Get_Jobs_Should_Return_Created_Jobs()
    {
        var client = _factory.CreateClient();

        var firstJob = await CreateJobAsync(client, "Replace rooftop unit");
        var secondJob = await CreateJobAsync(client, "Install condenser");

        var response = await client.GetAsync("/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JobReadResponse[]>();
        Assert.NotNull(body);

        Assert.Contains(body!, x => x.JobId == firstJob.JobId && x.Title == "Replace rooftop unit");
        Assert.Contains(body!, x => x.JobId == secondJob.JobId && x.Title == "Install condenser");
    }

    [Fact]
    public async Task Get_Job_Should_Return_NotFound_When_Job_Does_Not_Exist()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/jobs/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Start_Job_Should_Return_NotFound_When_Job_Does_Not_Exist()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"/jobs/{Guid.NewGuid()}/start", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Mark_Pending_Closeout_Should_Return_BadRequest_When_Job_Was_Not_Started()
    {
        var client = _factory.CreateClient();
        var createBody = await CreateJobAsync(client);

        var response = await client.PostAsync($"/jobs/{createBody.JobId}/mark-pending-closeout", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(errorBody);
        Assert.Equal("Jobs.CannotMarkPendingFromCurrentStatus", errorBody!.Error);
    }

    [Fact]
    public async Task Closeout_Should_Return_BadRequest_When_Job_Does_Not_Exist()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/jobs/{Guid.NewGuid()}/closeout",
            new CompleteJobCloseoutRequest(
                "Completed work",
                new[] { new ProofItemRequest(0, "Done") }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(errorBody);
        Assert.Equal("Jobs.NotFound", errorBody!.Error);
    }

    [Fact]
    public async Task Closeout_Should_Return_BadRequest_When_Job_Is_Not_Pending_Closeout()
    {
        var client = _factory.CreateClient();
        var createBody = await CreateJobAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/jobs/{createBody.JobId}/closeout",
            new CompleteJobCloseoutRequest(
                "Completed work",
                new[] { new ProofItemRequest(0, "Done") }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(errorBody);
        Assert.Equal("Jobs.JobMustBePendingCloseout", errorBody!.Error);
    }

    [Fact]
    public async Task Closeout_Should_Return_BadRequest_When_Proof_Items_Are_Empty()
    {
        var client = _factory.CreateClient();
        var createBody = await MoveJobToPendingCloseoutAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/jobs/{createBody.JobId}/closeout",
            new CompleteJobCloseoutRequest(
                "Completed work",
                Array.Empty<ProofItemRequest>()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(errorBody);
        Assert.Equal("Closeouts.ProofRequired", errorBody!.Error);

        var jobReadBody = await ReadJobAsync(client, createBody.JobId);
        Assert.Equal("PendingCloseout", jobReadBody.Status);
        Assert.Null(jobReadBody.ClosedAtUtc);
    }

    [Fact]
    public async Task Closeout_Should_Return_BadRequest_When_Proof_Items_Are_Null()
    {
        var client = _factory.CreateClient();
        var createBody = await MoveJobToPendingCloseoutAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/jobs/{createBody.JobId}/closeout",
            new NullableProofItemsCloseoutRequest(
                "Completed work",
                null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(errorBody);
        Assert.Equal("Closeouts.ProofRequired", errorBody!.Error);

        var jobReadBody = await ReadJobAsync(client, createBody.JobId);
        Assert.Equal("PendingCloseout", jobReadBody.Status);
        Assert.Null(jobReadBody.ClosedAtUtc);
    }

    [Fact]
    public async Task Closeout_Should_Return_BadRequest_When_Proof_Value_Is_Blank()
    {
        var client = _factory.CreateClient();
        var createBody = await MoveJobToPendingCloseoutAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/jobs/{createBody.JobId}/closeout",
            new CompleteJobCloseoutRequest(
                "Completed work",
                new[] { new ProofItemRequest(0, "   ") }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(errorBody);
        Assert.Equal("Closeouts.ProofValueRequired", errorBody!.Error);

        var jobReadBody = await ReadJobAsync(client, createBody.JobId);
        Assert.Equal("PendingCloseout", jobReadBody.Status);
        Assert.Null(jobReadBody.ClosedAtUtc);
    }

    [Fact]
    public async Task Get_Closeout_Should_Return_NotFound_When_Closeout_Does_Not_Exist()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/closeouts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Fact]
    public async Task Get_Closeouts_Should_Return_Created_Closeout_Records()
    {
        var client = _factory.CreateClient();
        var createBody = await MoveJobToPendingCloseoutAsync(client);

        var closeoutBody = await CompleteCloseoutAsync(
            client,
            createBody.JobId,
            "Completed rooftop unit replacement");

        var response = await client.GetAsync("/closeouts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CloseoutReadResponse[]>();
        Assert.NotNull(body);
        Assert.Contains(body!, x => x.CloseoutRecordId == closeoutBody.CloseoutRecordId);
    }

    [Fact]
    public async Task Get_Job_Closeouts_Should_Return_Only_Closeouts_For_Requested_Job()
    {
        var client = _factory.CreateClient();

        var firstJob = await MoveJobToPendingCloseoutAsync(client);
        var secondJob = await MoveJobToPendingCloseoutAsync(client);

        var firstCloseout = await CompleteCloseoutAsync(
            client,
            firstJob.JobId,
            "First job closeout");

        var secondCloseout = await CompleteCloseoutAsync(
            client,
            secondJob.JobId,
            "Second job closeout");

        var response = await client.GetAsync($"/jobs/{firstJob.JobId}/closeouts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CloseoutReadResponse[]>();
        Assert.NotNull(body);

        Assert.Contains(body!, x => x.CloseoutRecordId == firstCloseout.CloseoutRecordId);
        Assert.DoesNotContain(body!, x => x.CloseoutRecordId == secondCloseout.CloseoutRecordId);
    }

    [Fact]
    public async Task Get_Job_Closeouts_Should_Return_NotFound_When_Job_Does_Not_Exist()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/jobs/{Guid.NewGuid()}/closeouts");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static Task<CreateJobResponse> CreateJobAsync(HttpClient client)
    {
        return CreateJobAsync(client, "Replace rooftop unit");
    }

    private static async Task<CreateJobResponse> CreateJobAsync(HttpClient client, string title)
    {
        var createResponse = await client.PostAsJsonAsync(
            "/jobs",
            new CreateJobRequest(title));

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateJobResponse>();

        Assert.NotNull(createBody);
        Assert.NotEqual(Guid.Empty, createBody!.JobId);
        Assert.Equal(title.Trim(), createBody.Title);
        Assert.Equal("New", createBody.Status);

        return createBody;
    }

    private static async Task<CreateJobResponse> MoveJobToPendingCloseoutAsync(HttpClient client)
    {
        var createBody = await CreateJobAsync(client);

        var startResponse = await client.PostAsync($"/jobs/{createBody.JobId}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        var pendingResponse = await client.PostAsync($"/jobs/{createBody.JobId}/mark-pending-closeout", null);
        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);

        return createBody;
    }


    private static async Task<CompleteJobCloseoutResponse> CompleteCloseoutAsync(
        HttpClient client,
        Guid jobId,
        string summary)
    {
        var closeoutResponse = await client.PostAsJsonAsync(
            $"/jobs/{jobId}/closeout",
            new CompleteJobCloseoutRequest(
                summary,
                new[]
                {
                    new ProofItemRequest(0, "Work completed successfully")
                }));

        Assert.Equal(HttpStatusCode.OK, closeoutResponse.StatusCode);

        var closeoutBody = await closeoutResponse.Content.ReadFromJsonAsync<CompleteJobCloseoutResponse>();

        Assert.NotNull(closeoutBody);
        Assert.Equal(jobId, closeoutBody!.JobId);
        Assert.Equal("Closed", closeoutBody.JobStatus);

        return closeoutBody;
    }

    private static async Task<JobReadResponse> ReadJobAsync(HttpClient client, Guid jobId)
    {
        var response = await client.GetAsync($"/jobs/{jobId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JobReadResponse>();

        Assert.NotNull(body);

        return body!;
    }

    private sealed record CreateJobRequest(string Title);

    private sealed record CreateJobResponse(Guid JobId, string Title, string Status);

    private sealed record CompleteJobCloseoutResponse(Guid CloseoutRecordId, Guid JobId, string JobStatus);

    private sealed record CompleteJobCloseoutRequest(string Summary, ProofItemRequest[] ProofItems);

    private sealed record NullableProofItemsCloseoutRequest(string Summary, ProofItemRequest[]? ProofItems);

    private sealed record ProofItemRequest(int Type, string Value);

    private sealed record ErrorResponse(string Error, string Message);

    private sealed record JobReadResponse(
        Guid JobId,
        string Title,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? StartedAtUtc,
        DateTime? PendingCloseoutAtUtc,
        DateTime? ClosedAtUtc);

    private sealed record CloseoutReadResponse(
        Guid CloseoutRecordId,
        Guid JobId,
        string Summary,
        DateTime CreatedAtUtc,
        ProofItemReadResponse[] ProofItems);

    private sealed record ProofItemReadResponse(
        Guid ProofItemId,
        string Type,
        string Value,
        DateTime CreatedAtUtc);
}
