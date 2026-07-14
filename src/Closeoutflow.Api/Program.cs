using Closeoutflow.Api.Contracts;
using Closeoutflow.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Closeoutflow.Modules.Closeouts;
using Closeoutflow.Modules.Closeouts.Application;
using Closeoutflow.Modules.Jobs;
using Closeoutflow.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("CloseoutflowDb")
    ?? "Data Source=closeoutflow.db";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddScoped<IJobRepository, EfJobRepository>();
builder.Services.AddScoped<ICloseoutRecordRepository, EfCloseoutRecordRepository>();
builder.Services.AddScoped<ICompleteJobCloseoutPersistence, EfCompleteJobCloseoutPersistence>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddScoped<CompleteJobCloseoutHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "Healthy",
        service = "Closeoutflow.Api"
    });
});

app.MapGet("/health/ready", async (
    AppDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

    if (!canConnect)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new
    {
        status = "Ready",
        database = "Reachable"
    });
});

app.MapPost("/jobs", async (
    CreateJobRequest request,
    IJobRepository jobRepository,
    IDateTimeProvider dateTimeProvider,
    CancellationToken cancellationToken) =>
{
    var createResult = Job.Create(request.Title, dateTimeProvider.UtcNow);

    if (createResult.IsFailure)
    {
        return Results.BadRequest(ToErrorResponse(createResult.Error));
    }

    await jobRepository.AddAsync(createResult.Value, cancellationToken);

    return Results.Ok(ToJobReadModel(createResult.Value));
});


app.MapGet("/jobs", async (
    IJobRepository jobRepository,
    CancellationToken cancellationToken) =>
{
    var jobs = await jobRepository.ListAsync(cancellationToken);

    return Results.Ok(jobs.Select(ToJobReadModel));
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

    return Results.Ok(ToJobReadModel(job));
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
        return Results.BadRequest(ToErrorResponse(result.Error));
    }

    await jobRepository.UpdateAsync(job, cancellationToken);

    return Results.Ok(ToJobStatusResponse(job));
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
        return Results.BadRequest(ToErrorResponse(result.Error));
    }

    await jobRepository.UpdateAsync(job, cancellationToken);

    return Results.Ok(ToJobStatusResponse(job));
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
        return Results.BadRequest(ToErrorResponse(result.Error));
    }

    return Results.Ok(new CompleteJobCloseoutResponse(
        result.Value.CloseoutRecordId,
        result.Value.JobId,
        result.Value.JobStatus.ToString()));
});


app.MapGet("/closeouts", async (
    ICloseoutRecordRepository closeoutRecordRepository,
    CancellationToken cancellationToken) =>
{
    var closeouts = await closeoutRecordRepository.ListAsync(cancellationToken);

    return Results.Ok(closeouts.Select(ToCloseoutReadModel));
});

app.MapGet("/jobs/{id:guid}/closeouts", async (
    Guid id,
    IJobRepository jobRepository,
    ICloseoutRecordRepository closeoutRecordRepository,
    CancellationToken cancellationToken) =>
{
    var job = await jobRepository.GetByIdAsync(id, cancellationToken);

    if (job is null)
    {
        return Results.NotFound();
    }

    var closeouts = await closeoutRecordRepository.ListByJobIdAsync(id, cancellationToken);

    return Results.Ok(closeouts.Select(ToCloseoutReadModel));
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

    return Results.Ok(ToCloseoutReadModel(closeout));
});



static JobResponse ToJobReadModel(Job job)
{
    return new JobResponse(
        job.Id,
        job.Title,
        job.Status.ToString(),
        job.CreatedAtUtc,
        job.StartedAtUtc,
        job.PendingCloseoutAtUtc,
        job.ClosedAtUtc);
}

static JobStatusResponse ToJobStatusResponse(Job job)
{
    return new JobStatusResponse(
        job.Id,
        job.Status.ToString());
}

static ErrorResponse ToErrorResponse(Error error)
{
    return new ErrorResponse(
        error.Code,
        error.Message);
}

static CloseoutResponse ToCloseoutReadModel(CloseoutRecord closeout)
{
    return new CloseoutResponse(
        closeout.Id,
        closeout.JobId,
        closeout.Summary,
        closeout.CreatedAtUtc,
        closeout.ProofItems
            .Select(x => new ProofItemResponse(
                x.Id,
                x.Type.ToString(),
                x.Value,
                x.CreatedAtUtc))
            .ToArray());
}

app.Run();

public partial class Program { }