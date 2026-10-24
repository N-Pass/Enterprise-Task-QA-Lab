using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API in memory with an isolated SQLite in-memory database and a
/// controllable clock. Each factory instance has its own empty database.
/// </summary>
public sealed class TaskApiFactory : WebApplicationFactory<Program>
{
    // An in-memory SQLite database lives only while a connection to it is open,
    // so the factory keeps one open for its whole lifetime.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public TaskApiFactory()
    {
        _connection.Open();
    }

    public TestClock Clock { get; } = new(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // The real startup code still runs the migrations (against the in-memory database),
        // so the migrations themselves are exercised. No sample data is inserted.
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("Database:SeedSampleData", "false");

        builder.ConfigureTestServices(services =>
        {
            // Replace the file-based SQLite registration with the shared in-memory connection.
            // EF Core 9+ stores provider configuration in IDbContextOptionsConfiguration<T>.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
