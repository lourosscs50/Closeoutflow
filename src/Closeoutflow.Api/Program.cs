using Closeoutflow.Api.Contracts;
using Closeoutflow.Api.Repositories;
using Closeoutflow.Modules.Closeouts;
using Closeoutflow.Modules.Closeouts.Application;
using Closeoutflow.Modules.Jobs;
using Closeoutflow.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IJobRepository, InMemoryJobRepository>();
builder.Services.AddSingleton<ICloseoutRecordRepository, InMemoryCloseoutRecordRepository>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddScoped<CompleteJobCloseoutHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/jobs", async (
    CreateJobRequest request,
    IJobRepository jobRepository,
    IDateTimeProvider dateTimeProvider,
    CancellationToken cancellationToken) =>
{
    var createResult = Job.Create(request.Title, dateTimeProvider.UtcNow);

    if (createResult.IsFailure)
    {
        return Results.BadRequest(new
        {
            error = createResult.Error.Code,
            message = createResult.Error.Message
        });
    }

    await jobRepository.AddAsync(createResult.Value, cancellationToken);

    return Results.Ok(new
    {
        jobId = createResult.Value.Id,
        title = createResult.Value.Title,
        status = createResult.Value.Status.ToString()
    });
});

app.MapGet("/jobs/{id:guid}", async (
    Guid id,
    IJobRepository jobRepository,
    CancellationToken cancellationToken) =>
{
    var job = await jobRepository.GetByIdAsync(id, cancellationToken);

    if (job is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        jobId = job.Id,
        title = job.Title,
        status = job.Status.ToString(),
        createdAtUtc = job.CreatedAtUtc,
        startedAtUtc = job.StartedAtUtc,
        pendingCloseoutAtUtc = job.PendingCloseoutAtUtc,
        closedAtUtc = job.ClosedAtUtc
    });
});

app.MapPost("/jobs/{id:guid}/start", async (
    Guid id,
    IJobRepository jobRepository,
    IDateTimeProvider dateTimeProvider,
    CancellationToken cancellationToken) =>
{
    var job = await jobRepository.GetByIdAsync(id, cancellationToken);

    if (job is null)
    {
        return Results.NotFound();
    }

    var result = job.Start(dateTimeProvider.UtcNow);

    if (result.IsFailure)
    {
        return Results.BadRequest(new
        {
            error = result.Error.Code,
            message = result.Error.Message
        });
    }

    await jobRepository.UpdateAsync(job, cancellationToken);

    return Results.Ok(new
    {
        jobId = job.Id,
        status = job.Status.ToString()
    });
});

app.MapPost("/jobs/{id:guid}/mark-pending-closeout", async (
    Guid id,
    IJobRepository jobRepository,
    IDateTimeProvider dateTimeProvider,
    CancellationToken cancellationToken) =>
{
    var job = await jobRepository.GetByIdAsync(id, cancellationToken);

    if (job is null)
    {
        return Results.NotFound();
    }

    var result = job.MarkPendingCloseout(dateTimeProvider.UtcNow);

    if (result.IsFailure)
    {
        return Results.BadRequest(new
        {
            error = result.Error.Code,
            message = result.Error.Message
        });
    }

    await jobRepository.UpdateAsync(job, cancellationToken);

    return Results.Ok(new
    {
        jobId = job.Id,
        status = job.Status.ToString()
    });
});

app.MapPost("/jobs/{id:guid}/closeout", async (
    Guid id,
    CompleteJobCloseoutRequest request,
    CompleteJobCloseoutHandler handler,
    CancellationToken cancellationToken) =>
{
    var command = new CompleteJobCloseoutCommand(
        id,
        request.Summary,
        request.ProofItems?
            .Select(x => (x.Type, x.Value))
            .ToArray() ?? Array.Empty<(ProofItemType Type, string Value)>());

    var result = await handler.HandleAsync(command, cancellationToken);

    if (result.IsFailure)
    {
        return Results.BadRequest(new
        {
            error = result.Error.Code,
            message = result.Error.Message
        });
    }

    return Results.Ok(new
    {
        closeoutRecordId = result.Value.CloseoutRecordId,
        jobId = result.Value.JobId,
        jobStatus = result.Value.JobStatus.ToString()
    });
});

app.MapGet("/closeouts/{id:guid}", async (
    Guid id,
    ICloseoutRecordRepository closeoutRecordRepository,
    CancellationToken cancellationToken) =>
{
    var closeout = await closeoutRecordRepository.GetByIdAsync(id, cancellationToken);

    if (closeout is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        closeoutRecordId = closeout.Id,
        jobId = closeout.JobId,
        summary = closeout.Summary,
        createdAtUtc = closeout.CreatedAtUtc,
        proofItems = closeout.ProofItems.Select(x => new
        {
            proofItemId = x.Id,
            type = x.Type.ToString(),
            value = x.Value,
            createdAtUtc = x.CreatedAtUtc
        })
    });
});

app.Run();

public partial class Program { }