using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Closeoutflow.Api.Persistence;
using Closeoutflow.Modules.Closeouts;
using Closeoutflow.Modules.Closeouts.Application;
using Closeoutflow.Modules.Jobs;
using Closeoutflow.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Closeoutflow.Api.Tests;

public sealed class ConcurrentCloseoutContractTests
    : IClassFixture<CloseoutflowApiFactory>
{
    private readonly CloseoutflowApiFactory _factory;

    public ConcurrentCloseoutContractTests(
        CloseoutflowApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Closeout_Should_Allow_Only_One_Concurrent_Request()
    {
        using var concurrentFactory =
            _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<ConcurrentSaveGate>();

                    services.RemoveAll<
                        ICompleteJobCloseoutPersistence>();

                    services.AddScoped<
                        ICompleteJobCloseoutPersistence>(
                        serviceProvider =>
                            new CoordinatedCloseoutPersistence(
                                new EfCompleteJobCloseoutPersistence(
                                    serviceProvider
                                        .GetRequiredService<AppDbContext>()),
                                serviceProvider
                                    .GetRequiredService<
                                        ConcurrentSaveGate>()));
                });
            });

        using var setupClient = concurrentFactory.CreateClient();

        var jobId = await setupClient.CreateJobAsync(
            "Verify concurrent closeout handling");

        await setupClient.StartJobAsync(jobId);
        await setupClient.MarkPendingCloseoutAsync(jobId);

        using var firstClient = concurrentFactory.CreateClient();
        using var secondClient = concurrentFactory.CreateClient();

        var firstRequest = firstClient.PostAsJsonAsync(
            $"/jobs/{jobId}/closeout",
            new
            {
                summary = "First concurrent closeout.",
                proofItems = new[]
                {
                    new
                    {
                        type = 1,
                        value = "photo://first-concurrent-closeout"
                    }
                }
            });

        var secondRequest = secondClient.PostAsJsonAsync(
            $"/jobs/{jobId}/closeout",
            new
            {
                summary = "Second concurrent closeout.",
                proofItems = new[]
                {
                    new
                    {
                        type = 1,
                        value = "photo://second-concurrent-closeout"
                    }
                }
            });

        var responses = await Task.WhenAll(
            firstRequest,
            secondRequest);

        var successfulResponse = Assert.Single(
            responses,
                response =>
                    response.StatusCode == HttpStatusCode.OK);

        var conflictResponse = Assert.Single(
            responses,
                response =>
                    response.StatusCode == HttpStatusCode.Conflict);

        var successfulJson =
            await successfulResponse.Content
                .ReadFromJsonAsync<JsonElement>();

        Assert.True(
            successfulJson.TryGetProperty(
                "closeoutRecordId",
                out var closeoutRecordIdProperty));

        Assert.True(
            Guid.TryParse(
                closeoutRecordIdProperty.GetString(),
                out var successfulCloseoutRecordId));

        var conflictJson =
            await conflictResponse.Content
                .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "Closeouts.AlreadyExistsForJob",
            conflictJson.GetProperty("error").GetString());

        Assert.Equal(
            "A closeout record already exists for this job.",
            conflictJson.GetProperty("message").GetString());

        var jobResponse =
            await setupClient.GetAsync($"/jobs/{jobId}");

        Assert.Equal(
            HttpStatusCode.OK,
            jobResponse.StatusCode);

        var jobJson =
            await jobResponse.Content
                .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "Closed",
            jobJson.GetProperty("status").GetString());

        using var verificationScope =
            concurrentFactory.Services.CreateScope();

        var dbContext = verificationScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var persistedCloseout = await dbContext.CloseoutRecords
            .Include(closeout => closeout.ProofItems)
            .SingleAsync(closeout => closeout.JobId == jobId);

        Assert.Equal(
            successfulCloseoutRecordId,
            persistedCloseout.Id);

        Assert.Single(persistedCloseout.ProofItems);
    }

    private sealed class ConcurrentSaveGate
    {
        private const int ExpectedArrivals = 2;

        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int _arrivalCount;

        public async Task ArriveAndWaitAsync(
            CancellationToken cancellationToken)
        {
            var arrivalCount =
                Interlocked.Increment(ref _arrivalCount);

            if (arrivalCount == ExpectedArrivals)
            {
                _release.TrySetResult(true);
            }

            await _release.Task.WaitAsync(
                TimeSpan.FromSeconds(10),
                cancellationToken);
        }
    }

    private sealed class CoordinatedCloseoutPersistence
        : ICompleteJobCloseoutPersistence
    {
        private readonly ICompleteJobCloseoutPersistence _inner;
        private readonly ConcurrentSaveGate _gate;

        public CoordinatedCloseoutPersistence(
            ICompleteJobCloseoutPersistence inner,
            ConcurrentSaveGate gate)
        {
            _inner = inner;
            _gate = gate;
        }

        public async Task<Result> SaveAsync(
            Job job,
            CloseoutRecord closeoutRecord,
            CancellationToken cancellationToken = default)
        {
            await _gate.ArriveAndWaitAsync(cancellationToken);

            return await _inner.SaveAsync(
                job,
                closeoutRecord,
                cancellationToken);
        }
    }
}
