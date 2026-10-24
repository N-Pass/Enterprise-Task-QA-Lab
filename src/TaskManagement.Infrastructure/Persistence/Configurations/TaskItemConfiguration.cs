using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    // SQLite has no time zone information, so values come back as DateTimeKind.Unspecified.
    // This converter marks them as UTC on read, matching how they were written.
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        toDb => toDb,
        fromDb => DateTime.SpecifyKind(fromDb, DateTimeKind.Utc));

    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever(); // The domain generates the Guid.

        // Note: SQLite does not enforce text length; the domain is the real guard.
        // The lengths still document the schema and apply if the provider changes.
        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(TaskItem.TitleMaxLength);

        builder.Property(t => t.Description)
            .HasMaxLength(TaskItem.DescriptionMaxLength);

        builder.Property(t => t.AssignedTo)
            .HasMaxLength(TaskItem.AssignedToMaxLength);

        // Stored as text ("InProgress") so the database stays readable and
        // reordering enum members cannot silently corrupt existing rows.
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc)
            .HasConversion(UtcConverter)
            .IsRequired();

        builder.Property(t => t.UpdatedAtUtc)
            .HasConversion(UtcConverter)
            .IsRequired();
    }
}
