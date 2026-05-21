using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Closeoutflow.Api.Tests;

public class CloseoutWorkflowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CloseoutWorkflowTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Full_Closeout_Workflow_Should_Succeed()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsync("/jobs", null);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateJobResponse>();
        Assert.NotNull(createBody);
        Assert.NotEqual(Guid.Empty, createBody!.JobId);
        Assert.Equal("New", createBody.Status);

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

        var createResponse = await client.PostAsync("/jobs", null);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var createBody = await createResponse.Content.ReadFromJsonAsync<CreateJobResponse>();
        Assert.NotNull(createBody);

        var startResponse = await client.PostAsync($"/jobs/{createBody!.JobId}/start", null);
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

        var closeoutReadResponse = await client.GetAsync($"/closeouts/{closeoutBody!.CloseoutRecordId}");
        Assert.Equal(HttpStatusCode.OK, closeoutReadResponse.StatusCode);
    }

    private sealed record CreateJobResponse(Guid JobId, string Status);

    private sealed record CompleteJobCloseoutResponse(Guid CloseoutRecordId, Guid JobId, string JobStatus);

    private sealed record CompleteJobCloseoutRequest(string Summary, ProofItemRequest[] ProofItems);

    private sealed record ProofItemRequest(int Type, string Value);
}