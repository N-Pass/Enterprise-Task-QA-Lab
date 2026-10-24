using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Abstractions;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the EF Core context and repository implementations.</summary>
    /// <param name="baseDirectory">Directory that relative SQLite file paths are resolved against.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string baseDirectory)
    {
        var resolvedConnectionString = ResolveSqliteFilePath(connectionString, baseDirectory);

        // DbContext is registered as scoped: one instance per HTTP request.
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(resolvedConnectionString));
        services.AddScoped<ITaskRepository, TaskRepository>();

        return services;
    }

    // SQLite resolves relative paths against the current working directory, which changes
    // depending on where "dotnet run" is executed. Anchoring to a known directory keeps
    // a single database file between runs.
    private static string ResolveSqliteFilePath(string connectionString, string baseDirectory)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;

        var isFilePath = !string.IsNullOrEmpty(dataSource)
            && dataSource != ":memory:"
            && !dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase);

        if (isFilePath && !Path.IsPathRooted(dataSource))
        {
            builder.DataSource = Path.Combine(baseDirectory, dataSource);
        }

        return builder.ToString();
    }
}
