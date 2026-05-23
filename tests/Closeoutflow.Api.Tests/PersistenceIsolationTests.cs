using System.Net;
using System.Net.Http.Json;

namespace Closeoutflow.Api.Tests;

public sealed class PersistenceIsolationTests
{
    [Fact]
    public async Task Separate_Test_Factories_Should_Not_Share_Persisted_Jobs()
    {
        await using var firstFactory = new CloseoutflowApiFactory();
        await using var secondFactory = new CloseoutflowApiFactory();

        var firstClient = firstFactory.CreateClient();
        var secondClient = secondFactory.CreateClient();

        var createResponse = await firstClient.PostAsJsonAsync(
            "/jobs",
            new CreateJobRequest("Factory isolated job"));

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var firstJobsResponse = await firstClient.GetAsync("/jobs");
        Assert.Equal(HttpStatusCode.OK, firstJobsResponse.StatusCode);

        var firstJobs = await firstJobsResponse.Content.ReadFromJsonAsync<JobReadResponse[]>();
        Assert.NotNull(firstJobs);
        Assert.Contains(firstJobs!, x => x.Title == "Factory isolated job");

        var secondJobsResponse = await secondClient.GetAsync("/jobs");
        Assert.Equal(HttpStatusCode.OK, secondJobsResponse.StatusCode);

        var secondJobs = await secondJobsResponse.Content.ReadFromJsonAsync<JobReadResponse[]>();
        Assert.NotNull(secondJobs);
        Assert.DoesNotContain(secondJobs!, x => x.Title == "Factory isolated job");
    }

    private sealed record CreateJobRequest(string Title);

    private sealed record JobReadResponse(
        Guid JobId,
        string Title,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? StartedAtUtc,
        DateTime? PendingCloseoutAtUtc,
        DateTime? ClosedAtUtc);
}
