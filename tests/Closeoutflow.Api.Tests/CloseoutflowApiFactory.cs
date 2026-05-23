using Closeoutflow.Api.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Closeoutflow.Api.Tests;

public sealed class CloseoutflowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath;

    public CloseoutflowApiFactory()
    {
        _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"closeoutflow-tests-{Guid.NewGuid():N}.db");

        DeleteDatabaseFiles();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        DeleteDatabaseFiles();
    }

    private void DeleteDatabaseFiles()
    {
        DeleteFileIfExists(_databasePath);
        DeleteFileIfExists($"{_databasePath}-shm");
        DeleteFileIfExists($"{_databasePath}-wal");
    }

    private static void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
