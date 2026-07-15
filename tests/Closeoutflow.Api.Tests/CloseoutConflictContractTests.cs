using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Closeoutflow.Modules.Closeouts;
using Closeoutflow.Modules.Closeouts.Application;
using Closeoutflow.Modules.Jobs;
using Closeoutflow.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Closeoutflow.Api.Tests;

public sealed class CloseoutConflictContractTests
    : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public CloseoutConflictContractTests(
        CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Closeout_Should_Return_Conflict_When_Job_Already_Has_Closeout()
    {
        using var conflictFactory =
            _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<
                        ICompleteJobCloseoutPersistence>();

                    services.AddScoped<
                        ICompleteJobCloseoutPersistence>(
                        _ => new DuplicateCloseoutPersistence());
                });
            });

        var client = conflictFactory.CreateClient();

        var jobId = await client.CreateJobAsync(
            "Inspect loading dock controls");

        await client.StartJobAsync(jobId);
        await client.MarkPendingCloseoutAsync(jobId);

        var response = await client.PostAsJsonAsync(
            $"/jobs/{jobId}/closeout",
            new
            {
                summary = "Inspection completed.",
                proofItems = new[]
                {
                    new
                    {
                        type = 1,
                        value = "photo://loading-dock-controls"
                    }
                }
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);

        var json =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "Closeouts.AlreadyExistsForJob",
            json.GetProperty("error").GetString());

        Assert.Equal(
            "A closeout record already exists for this job.",
            json.GetProperty("message").GetString());

        var jobResponse =
            await client.GetAsync($"/jobs/{jobId}");

        Assert.Equal(
            HttpStatusCode.OK,
            jobResponse.StatusCode);

        var jobJson =
            await jobResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "PendingCloseout",
            jobJson.GetProperty("status").GetString());
    }

    private sealed class DuplicateCloseoutPersistence
        : ICompleteJobCloseoutPersistence
    {
        public Task<Result> SaveAsync(
            Job job,
            CloseoutRecord closeoutRecord,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Result.Failure(
                    CloseoutErrors.AlreadyExistsForJob));
        }
    }
}
