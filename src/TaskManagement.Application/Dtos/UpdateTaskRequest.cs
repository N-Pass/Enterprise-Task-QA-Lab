using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Dtos;

/// <summary>
/// Full replacement of the editable task details (PUT semantics).
/// Status is not included; it changes only through the status endpoint.
/// </summary>
public sealed record UpdateTaskRequest
{
    [Required]
    [StringLength(TaskItem.TitleMaxLength)]
    public string Title { get; init; } = string.Empty;

    [StringLength(TaskItem.DescriptionMaxLength)]
    public string? Description { get; init; }

    // Nullable so that a missing value is reported instead of silently becoming Low.
    [Required]
    public TaskPriority? Priority { get; init; }

    [StringLength(TaskItem.AssignedToMaxLength)]
    public string? AssignedTo { get; init; }
}
