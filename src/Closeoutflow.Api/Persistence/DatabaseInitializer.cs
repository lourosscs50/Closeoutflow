using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Closeoutflow.Api.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        AppDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        const string createUniqueIndexSql = """
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_closeout_records_JobId"
            ON "closeout_records" ("JobId");
            """;

        try
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                createUniqueIndexSql,
                cancellationToken);
        }
        catch (SqliteException exception)
            when (exception.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException(
                "The closeout database contains multiple closeout records for the same job.",
                exception);
        }
    }
}
