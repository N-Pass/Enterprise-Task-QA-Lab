using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence;

/// <summary>Applies pending migrations and optionally inserts development sample data.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        AppDbContext context,
        TimeProvider timeProvider,
        bool seedSampleData,
        CancellationToken cancellationToken = default)
    {
        // Idempotent: creates the database if missing and applies only pending migrations.
        await context.Database.MigrateAsync(cancellationToken);

        if (!seedSampleData || await context.Tasks.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var setUpPipeline = new TaskItem(
            "Set up CI pipeline", "Build and test on every push.", TaskPriority.High, "Alex", now);
        setUpPipeline.ChangeStatus(TaskItemStatus.InProgress, now);

        var writeDocs = new TaskItem(
            "Write API documentation", "Describe endpoints and example payloads.", TaskPriority.Medium, null, now);

        var reviewDesign = new TaskItem(
            "Review UI accessibility", "Check labels, focus order and contrast.", TaskPriority.Low, "Sam", now);
        reviewDesign.ChangeStatus(TaskItemStatus.Done, now);

        context.Tasks.AddRange(setUpPipeline, writeDocs, reviewDesign);
        await context.SaveChangesAsync(cancellationToken);
    }
}
