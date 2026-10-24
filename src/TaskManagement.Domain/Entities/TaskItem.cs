using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.Domain.Entities;

/// <summary>
/// A unit of work tracked by the system. All state changes go through methods
/// that enforce the business rules, so an invalid task cannot be created.
/// </summary>
public class TaskItem
{
    public const int TitleMaxLength = 120;
    public const int DescriptionMaxLength = 1000;
    public const int AssignedToMaxLength = 100;

    // Required by EF Core to materialize entities from the database.
    private TaskItem()
    {
    }

    public TaskItem(string title, string? description, TaskPriority priority, string? assignedTo, DateTime utcNow)
    {
        RequireUtc(utcNow);

        Id = Guid.NewGuid();
        Title = NormalizeTitle(title);
        Description = NormalizeOptionalText(description, nameof(Description), DescriptionMaxLength);
        Priority = RequireDefined(priority, nameof(Priority));
        AssignedTo = NormalizeOptionalText(assignedTo, nameof(AssignedTo), AssignedToMaxLength);
        Status = TaskItemStatus.ToDo;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    // Private setters: the identifier and timestamps can only change through domain methods.
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskItemStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public string? AssignedTo { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>Replaces the editable details of the task. Status is changed separately.</summary>
    public void Update(string title, string? description, TaskPriority priority, string? assignedTo, DateTime utcNow)
    {
        RequireUtc(utcNow);

        // Validate everything before mutating, so a failed update leaves the task unchanged.
        var newTitle = NormalizeTitle(title);
        var newDescription = NormalizeOptionalText(description, nameof(Description), DescriptionMaxLength);
        var newPriority = RequireDefined(priority, nameof(Priority));
        var newAssignedTo = NormalizeOptionalText(assignedTo, nameof(AssignedTo), AssignedToMaxLength);

        Title = newTitle;
        Description = newDescription;
        Priority = newPriority;
        AssignedTo = newAssignedTo;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Moves the task to another status. Any transition is allowed; setting the
    /// current status again is a no-op and does not touch the timestamp.
    /// </summary>
    public void ChangeStatus(TaskItemStatus newStatus, DateTime utcNow)
    {
        RequireUtc(utcNow);
        RequireDefined(newStatus, nameof(Status));

        if (newStatus == Status)
        {
            return;
        }

        Status = newStatus;
        UpdatedAtUtc = utcNow;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainValidationException(nameof(Title), "Title is required.");
        }

        var trimmed = title.Trim();
        if (trimmed.Length > TitleMaxLength)
        {
            throw new DomainValidationException(nameof(Title), $"Title must not exceed {TitleMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalText(string? value, string propertyName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainValidationException(propertyName, $"{propertyName} must not exceed {maxLength} characters.");
        }

        return trimmed;
    }

    private static TEnum RequireDefined<TEnum>(TEnum value, string propertyName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainValidationException(propertyName, $"'{value}' is not a valid {propertyName}.");
        }

        return value;
    }

    private static void RequireUtc(DateTime utcNow)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamps must be in UTC.", nameof(utcNow));
        }
    }
}
