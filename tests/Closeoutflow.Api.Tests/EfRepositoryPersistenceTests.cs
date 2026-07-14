using Microsoft.Data.Sqlite;
using Closeoutflow.Api.Persistence;
using Closeoutflow.Modules.Closeouts;
using Closeoutflow.Modules.Closeouts.Application;
using Closeoutflow.Modules.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Closeoutflow.Api.Tests;

public sealed class EfRepositoryPersistenceTests
{
    [Fact]
    public async Task JobRepository_AddAndGet_Should_Preserve_Domain_State()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfJobRepository(database.Context);

        var createdAt = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var startedAt = createdAt.AddHours(1);
        var pendingAt = createdAt.AddHours(2);
        var closedAt = createdAt.AddHours(3);

        var job = Job.Rehydrate(
            Guid.NewGuid(),
            "Repair loading dock",
            JobStatus.Closed,
            createdAt,
            startedAt,
            pendingAt,
            closedAt);

        await repository.AddAsync(job);

        var persisted = await repository.GetByIdAsync(job.Id);

        Assert.NotNull(persisted);
        Assert.Equal(job.Id, persisted!.Id);
        Assert.Equal("Repair loading dock", persisted.Title);
        Assert.Equal(JobStatus.Closed, persisted.Status);
        Assert.Equal(createdAt, persisted.CreatedAtUtc);
        Assert.Equal(startedAt, persisted.StartedAtUtc);
        Assert.Equal(pendingAt, persisted.PendingCloseoutAtUtc);
        Assert.Equal(closedAt, persisted.ClosedAtUtc);
    }

    [Fact]
    public async Task JobRepository_Update_Should_Persist_Lifecycle_Changes()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfJobRepository(database.Context);

        var createdAt = new DateTime(2026, 5, 2, 8, 0, 0, DateTimeKind.Utc);
        var job = Job.Create("Replace entry lock", createdAt).Value;

        await repository.AddAsync(job);

        var startedAt = createdAt.AddMinutes(30);
        var pendingAt = createdAt.AddHours(1);
        var closedAt = createdAt.AddHours(2);

        Assert.True(job.Start(startedAt).IsSuccess);
        Assert.True(job.MarkPendingCloseout(pendingAt).IsSuccess);
        Assert.True(job.Close(closedAt).IsSuccess);

        await repository.UpdateAsync(job);

        var persisted = await repository.GetByIdAsync(job.Id);

        Assert.NotNull(persisted);
        Assert.Equal(JobStatus.Closed, persisted!.Status);
        Assert.Equal(startedAt, persisted.StartedAtUtc);
        Assert.Equal(pendingAt, persisted.PendingCloseoutAtUtc);
        Assert.Equal(closedAt, persisted.ClosedAtUtc);
    }

    [Fact]
    public async Task JobRepository_List_Should_Order_Newest_First()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfJobRepository(database.Context);

        var older = Job.Create(
            "Older job",
            new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc)).Value;

        var newer = Job.Create(
            "Newer job",
            new DateTime(2026, 5, 2, 8, 0, 0, DateTimeKind.Utc)).Value;

        await repository.AddAsync(older);
        await repository.AddAsync(newer);

        var jobs = await repository.ListAsync();

        Assert.Collection(
            jobs,
            first => Assert.Equal(newer.Id, first.Id),
            second => Assert.Equal(older.Id, second.Id));
    }

    [Fact]
    public async Task JobRepository_Add_Should_Reject_Duplicate_Id()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfJobRepository(database.Context);

        var job = Job.Create(
            "Duplicate job",
            new DateTime(2026, 5, 3, 8, 0, 0, DateTimeKind.Utc)).Value;

        await repository.AddAsync(job);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.AddAsync(job));

        Assert.Contains(job.Id.ToString(), exception.Message);
    }

    [Fact]
    public async Task JobRepository_Update_Should_Reject_Missing_Job()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfJobRepository(database.Context);

        var missingJob = Job.Create(
            "Missing job",
            new DateTime(2026, 5, 4, 8, 0, 0, DateTimeKind.Utc)).Value;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.UpdateAsync(missingJob));

        Assert.Contains(missingJob.Id.ToString(), exception.Message);
    }

    [Fact]
    public async Task CloseoutRepository_AddAndGet_Should_Preserve_Proof_Items()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfCloseoutRecordRepository(database.Context);

        var jobId = Guid.NewGuid();
        var closeoutId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 5, 5, 8, 0, 0, DateTimeKind.Utc);

        var earlierProof = ProofItem.Rehydrate(
            Guid.NewGuid(),
            ProofItemType.Note,
            "Technician note",
            createdAt.AddMinutes(-2));

        var laterProof = ProofItem.Rehydrate(
            Guid.NewGuid(),
            ProofItemType.Photo,
            "photo://completed-work",
            createdAt.AddMinutes(-1));

        var closeout = CloseoutRecord.Rehydrate(
            closeoutId,
            jobId,
            "Work completed and verified.",
            createdAt,
            new[] { laterProof, earlierProof });

        await repository.AddAsync(closeout);

        var persisted = await repository.GetByIdAsync(closeoutId);

        Assert.NotNull(persisted);
        Assert.Equal(closeoutId, persisted!.Id);
        Assert.Equal(jobId, persisted.JobId);
        Assert.Equal("Work completed and verified.", persisted.Summary);
        Assert.Equal(createdAt, persisted.CreatedAtUtc);

        Assert.Collection(
            persisted.ProofItems,
            first =>
            {
                Assert.Equal(earlierProof.Id, first.Id);
                Assert.Equal(ProofItemType.Note, first.Type);
                Assert.Equal("Technician note", first.Value);
            },
            second =>
            {
                Assert.Equal(laterProof.Id, second.Id);
                Assert.Equal(ProofItemType.Photo, second.Type);
                Assert.Equal("photo://completed-work", second.Value);
            });
    }

    [Fact]
    public async Task CloseoutRepository_Lists_Should_Order_And_Filter_Correctly()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfCloseoutRecordRepository(database.Context);

        var firstJobId = Guid.NewGuid();
        var secondJobId = Guid.NewGuid();
        var thirdJobId = Guid.NewGuid();

        var older = CreateCloseout(
            firstJobId,
            "Older closeout",
            new DateTime(2026, 5, 6, 8, 0, 0, DateTimeKind.Utc));

        var newerForSecondJob = CreateCloseout(
            secondJobId,
            "Newer closeout for second job",
            new DateTime(2026, 5, 7, 8, 0, 0, DateTimeKind.Utc));

        var newestForThirdJob = CreateCloseout(
            thirdJobId,
            "Newest closeout for third job",
            new DateTime(2026, 5, 8, 8, 0, 0, DateTimeKind.Utc));

        await repository.AddAsync(older);
        await repository.AddAsync(newerForSecondJob);
        await repository.AddAsync(newestForThirdJob);

        var all = await repository.ListAsync();

        Assert.Collection(
            all,
            first => Assert.Equal(newestForThirdJob.Id, first.Id),
            second => Assert.Equal(newerForSecondJob.Id, second.Id),
            third => Assert.Equal(older.Id, third.Id));

        var firstJobCloseouts = await repository.ListByJobIdAsync(firstJobId);

        var firstJobCloseout = Assert.Single(firstJobCloseouts);

        Assert.Equal(older.Id, firstJobCloseout.Id);
        Assert.Equal(firstJobId, firstJobCloseout.JobId);
    }

    [Fact]
    public async Task CloseoutRepository_Add_Should_Reject_Duplicate_Id()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfCloseoutRecordRepository(database.Context);

        var closeout = CreateCloseout(
            Guid.NewGuid(),
            "Duplicate closeout",
            new DateTime(2026, 5, 9, 8, 0, 0, DateTimeKind.Utc));

        await repository.AddAsync(closeout);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.AddAsync(closeout));

        Assert.Contains(closeout.Id.ToString(), exception.Message);
    }


    [Fact]
    public async Task CompleteJobCloseoutPersistence_Save_Should_Persist_Job_And_Closeout_Together()
    {
        await using var database = await TestDatabase.CreateAsync();

        var jobs = new EfJobRepository(database.Context);
        var closeouts =
            new EfCloseoutRecordRepository(database.Context);

        var persistence =
            new EfCompleteJobCloseoutPersistence(database.Context);

        var now = new DateTime(
            2026,
            5,
            10,
            12,
            0,
            0,
            DateTimeKind.Utc);

        var job = Job.Create(
            "Inspect emergency lighting",
            now.AddHours(-3)).Value;

        Assert.True(
            job.Start(now.AddHours(-2)).IsSuccess);

        Assert.True(
            job.MarkPendingCloseout(now.AddHours(-1)).IsSuccess);

        await jobs.AddAsync(job);

        var closeout = CloseoutRecord.Create(
            job.Id,
            "Inspection completed.",
            new[]
            {
                (
                    ProofItemType.Photo,
                    "photo://emergency-lighting")
            },
            now).Value;

        Assert.True(job.Close(now).IsSuccess);

        await persistence.SaveAsync(job, closeout);

        var persistedJob = await jobs.GetByIdAsync(job.Id);
        var persistedCloseout =
            await closeouts.GetByIdAsync(closeout.Id);

        Assert.NotNull(persistedJob);
        Assert.Equal(
            JobStatus.Closed,
            persistedJob!.Status);

        Assert.Equal(
            now,
            persistedJob.ClosedAtUtc);

        Assert.NotNull(persistedCloseout);
        Assert.Equal(
            job.Id,
            persistedCloseout!.JobId);

        Assert.Equal(
            "Inspection completed.",
            persistedCloseout.Summary);

        Assert.Single(persistedCloseout.ProofItems);
    }

    [Fact]
    public async Task CompleteJobCloseoutPersistence_Save_Should_Roll_Back_All_Changes_When_Save_Fails()
    {
        await using var database = await TestDatabase.CreateAsync();

        var jobs = new EfJobRepository(database.Context);
        var closeouts =
            new EfCloseoutRecordRepository(database.Context);

        var persistence =
            new EfCompleteJobCloseoutPersistence(database.Context);

        var now = new DateTime(
            2026,
            5,
            11,
            12,
            0,
            0,
            DateTimeKind.Utc);

        var job = Job.Create(
            "Repair warehouse door",
            now.AddHours(-3)).Value;

        Assert.True(
            job.Start(now.AddHours(-2)).IsSuccess);

        Assert.True(
            job.MarkPendingCloseout(now.AddHours(-1)).IsSuccess);

        await jobs.AddAsync(job);

        var duplicateProofId = Guid.NewGuid();

        var existingCloseout = CloseoutRecord.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Existing closeout",
            now.AddMinutes(-30),
            new[]
            {
                ProofItem.Rehydrate(
                    duplicateProofId,
                    ProofItemType.Note,
                    "Existing proof",
                    now.AddMinutes(-30))
            });

        await closeouts.AddAsync(existingCloseout);

        database.Context.ChangeTracker.Clear();

        var attemptedCloseout = CloseoutRecord.Rehydrate(
            Guid.NewGuid(),
            job.Id,
            "Attempted closeout",
            now,
            new[]
            {
                ProofItem.Rehydrate(
                    duplicateProofId,
                    ProofItemType.Photo,
                    "photo://duplicate-proof-id",
                    now)
            });

        Assert.True(job.Close(now).IsSuccess);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => persistence.SaveAsync(
                job,
                attemptedCloseout));

        database.Context.ChangeTracker.Clear();

        var persistedJob = await jobs.GetByIdAsync(job.Id);

        var persistedAttempt =
            await closeouts.GetByIdAsync(
                attemptedCloseout.Id);

        var persistedExisting =
            await closeouts.GetByIdAsync(
                existingCloseout.Id);

        Assert.NotNull(persistedJob);
        Assert.Equal(
            JobStatus.PendingCloseout,
            persistedJob!.Status);

        Assert.Null(persistedJob.ClosedAtUtc);
        Assert.Null(persistedAttempt);
        Assert.NotNull(persistedExisting);
    }



    [Fact]
    public async Task Unique_Job_Closeout_Index_Should_Allow_Only_One_Competing_Insert_Across_Contexts()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();

        var jobId = Guid.NewGuid();
        var createdAtUtc = new DateTime(
            2026,
            5,
            14,
            8,
            0,
            0,
            DateTimeKind.Utc);

        firstContext.CloseoutRecords.Add(
            new CloseoutRecordRow
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                Summary = "First competing closeout",
                CreatedAtUtc = createdAtUtc
            });

        secondContext.CloseoutRecords.Add(
            new CloseoutRecordRow
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                Summary = "Second competing closeout",
                CreatedAtUtc = createdAtUtc.AddSeconds(1)
            });

        var exceptions = await Task.WhenAll(
            CaptureExceptionAsync(
                async () =>
                {
                    await firstContext.SaveChangesAsync();
                }),
            CaptureExceptionAsync(
                async () =>
                {
                    await secondContext.SaveChangesAsync();
                }));

        Assert.Equal(
            1,
            exceptions.Count(exception => exception is null));

        var conflict = Assert.Single(
            exceptions.OfType<DbUpdateException>());

        var sqliteException =
            Assert.IsType<SqliteException>(
                conflict.InnerException);

        Assert.Equal(19, sqliteException.SqliteErrorCode);

        Assert.Contains(
            "closeout_records.JobId",
            sqliteException.Message);

        database.Context.ChangeTracker.Clear();

        var persistedCloseouts =
            await database.Context.CloseoutRecords
                .Where(x => x.JobId == jobId)
                .ToListAsync();

        Assert.Single(persistedCloseouts);
    }

    [Fact]
    public async Task DatabaseInitializer_Should_Reject_Existing_Duplicate_Job_Closeouts()
    {
        await using var database = await TestDatabase.CreateAsync();

        await database.Context.Database.ExecuteSqlRawAsync(
            """
            DROP INDEX "UX_closeout_records_JobId";
            """);

        var jobId = Guid.NewGuid();

        database.Context.CloseoutRecords.AddRange(
            new CloseoutRecordRow
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                Summary = "Existing first closeout",
                CreatedAtUtc = new DateTime(
                    2026,
                    5,
                    15,
                    8,
                    0,
                    0,
                    DateTimeKind.Utc)
            },
            new CloseoutRecordRow
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                Summary = "Existing duplicate closeout",
                CreatedAtUtc = new DateTime(
                    2026,
                    5,
                    15,
                    9,
                    0,
                    0,
                    DateTimeKind.Utc)
            });

        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => DatabaseInitializer.InitializeAsync(
                    database.Context));

        Assert.Equal(
            "The closeout database contains multiple closeout records for the same job.",
            exception.Message);

        var sqliteException =
            Assert.IsType<SqliteException>(
                exception.InnerException);

        Assert.Equal(19, sqliteException.SqliteErrorCode);
    }

    [Fact]
    public async Task DatabaseInitializer_Should_Add_Unique_Job_Closeout_Index_To_Existing_Database()
    {
        await using var database = await TestDatabase.CreateAsync();

        await database.Context.Database.ExecuteSqlRawAsync(
            """
            DROP INDEX "UX_closeout_records_JobId";
            """);

        await DatabaseInitializer.InitializeAsync(
            database.Context);

        var jobId = Guid.NewGuid();

        database.Context.CloseoutRecords.Add(
            new CloseoutRecordRow
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                Summary = "First closeout",
                CreatedAtUtc = new DateTime(
                    2026,
                    5,
                    12,
                    8,
                    0,
                    0,
                    DateTimeKind.Utc)
            });

        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        database.Context.CloseoutRecords.Add(
            new CloseoutRecordRow
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                Summary = "Duplicate closeout",
                CreatedAtUtc = new DateTime(
                    2026,
                    5,
                    12,
                    9,
                    0,
                    0,
                    DateTimeKind.Utc)
            });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task CompleteJobCloseoutPersistence_Save_Should_Return_Failure_When_Job_Already_Has_Closeout()
    {
        await using var database = await TestDatabase.CreateAsync();

        var jobs = new EfJobRepository(database.Context);

        var closeouts =
            new EfCloseoutRecordRepository(database.Context);

        var persistence =
            new EfCompleteJobCloseoutPersistence(database.Context);

        var now = new DateTime(
            2026,
            5,
            13,
            12,
            0,
            0,
            DateTimeKind.Utc);

        var job = Job.Create(
            "Inspect loading dock controls",
            now.AddHours(-3)).Value;

        Assert.True(
            job.Start(now.AddHours(-2)).IsSuccess);

        Assert.True(
            job.MarkPendingCloseout(now.AddHours(-1)).IsSuccess);

        await jobs.AddAsync(job);

        var firstPendingJob =
            await jobs.GetByIdAsync(job.Id);

        var stalePendingJob =
            await jobs.GetByIdAsync(job.Id);

        Assert.NotNull(firstPendingJob);
        Assert.NotNull(stalePendingJob);

        var firstCloseout = CloseoutRecord.Create(
            job.Id,
            "First closeout",
            new[]
            {
                (
                    ProofItemType.Photo,
                    "photo://first-closeout")
            },
            now).Value;

        Assert.True(
            firstPendingJob!.Close(now).IsSuccess);

        var firstResult = await persistence.SaveAsync(
            firstPendingJob,
            firstCloseout);

        Assert.True(firstResult.IsSuccess);

        var attemptedCloseout = CloseoutRecord.Create(
            job.Id,
            "Attempted duplicate closeout",
            new[]
            {
                (
                    ProofItemType.Note,
                    "Duplicate submission")
            },
            now.AddMinutes(1)).Value;

        Assert.True(
            stalePendingJob!.Close(
                now.AddMinutes(1)).IsSuccess);

        var duplicateResult = await persistence.SaveAsync(
            stalePendingJob,
            attemptedCloseout);

        Assert.True(duplicateResult.IsFailure);
        Assert.Equal(
            CloseoutErrors.AlreadyExistsForJob,
            duplicateResult.Error);

        database.Context.ChangeTracker.Clear();

        var persistedJob =
            await jobs.GetByIdAsync(job.Id);

        var persistedCloseouts =
            await closeouts.ListByJobIdAsync(job.Id);

        Assert.NotNull(persistedJob);
        Assert.Equal(
            JobStatus.Closed,
            persistedJob!.Status);

        Assert.Equal(now, persistedJob.ClosedAtUtc);

        Assert.Single(persistedCloseouts);
        Assert.Equal(
            firstCloseout.Id,
            persistedCloseouts.Single().Id);
    }


    private static async Task<Exception?> CaptureExceptionAsync(
        Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static CloseoutRecord CreateCloseout(
        Guid jobId,
        string summary,
        DateTime createdAtUtc)
    {
        return CloseoutRecord.Create(
            jobId,
            summary,
            new[]
            {
                (
                    ProofItemType.Photo,
                    $"photo://{Guid.NewGuid():N}")
            },
            createdAtUtc).Value;
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string _databasePath;

        private TestDatabase(
            string databasePath,
            AppDbContext context)
        {
            _databasePath = databasePath;
            Context = context;
        }

        internal AppDbContext Context { get; }

        internal AppDbContext CreateContext()
        {
            var options =
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(
                        $"Data Source={_databasePath};Pooling=False")
                    .Options;

            return new AppDbContext(options);
        }

        internal static async Task<TestDatabase> CreateAsync()
        {
            var databasePath = Path.Combine(
                Path.GetTempPath(),
                $"closeoutflow-repository-tests-{Guid.NewGuid():N}.db");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath};Pooling=False")
                .Options;

            var context = new AppDbContext(options);

            await context.Database.EnsureCreatedAsync();

            return new TestDatabase(databasePath, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();

            DeleteIfExists(_databasePath);
            DeleteIfExists($"{_databasePath}-shm");
            DeleteIfExists($"{_databasePath}-wal");
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
