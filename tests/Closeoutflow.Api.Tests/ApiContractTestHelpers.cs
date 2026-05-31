using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Closeoutflow.Api.Tests;

internal static class ApiContractTestHelpers
{
    internal static async Task<Guid> CreateJobAsync(
        this HttpClient client,
        string title)
    {
        var response = await client.PostAsJsonAsync("/jobs", new
        {
            title
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.TryGetProperty("jobId", out var jobId));
        Assert.True(Guid.TryParse(jobId.GetString(), out var parsedJobId));

        return parsedJobId;
    }

    internal static async Task StartJobAsync(
        this HttpClient client,
        Guid jobId)
    {
        var response = await client.PostAsync($"/jobs/{jobId}/start", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    internal static async Task MarkPendingCloseoutAsync(
        this HttpClient client,
        Guid jobId)
    {
        var response = await client.PostAsync($"/jobs/{jobId}/mark-pending-closeout", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    internal static async Task<Guid> CloseoutJobAsync(
        this HttpClient client,
        Guid jobId,
        string summary,
        string proofValue)
    {
        var response = await client.PostAsJsonAsync($"/jobs/{jobId}/closeout", new
        {
            summary,
            proofItems = new[]
            {
                new
                {
                    type = 1,
                    value = proofValue
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.TryGetProperty("closeoutRecordId", out var closeoutRecordId));
        Assert.True(Guid.TryParse(closeoutRecordId.GetString(), out var parsedCloseoutRecordId));

        return parsedCloseoutRecordId;
    }

    internal static async Task<(Guid JobId, Guid CloseoutRecordId)> CreateClosedOutJobAsync(
        this HttpClient client,
        string title,
        string summary,
        string proofValue)
    {
        var jobId = await client.CreateJobAsync(title);

        await client.StartJobAsync(jobId);
        await client.MarkPendingCloseoutAsync(jobId);

        var closeoutRecordId = await client.CloseoutJobAsync(
            jobId,
            summary,
            proofValue);

        return (jobId, closeoutRecordId);
    }
}
