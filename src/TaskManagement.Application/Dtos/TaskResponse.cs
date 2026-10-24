using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Dtos;

/// <summary>Task representation returned by the API. Decouples the HTTP contract from the entity.</summary>
public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    string? AssignedTo,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc)
{
    public static TaskResponse FromEntity(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.Priority,
        task.AssignedTo,
        task.CreatedAtUtc,
        task.UpdatedAtUtc);
}
