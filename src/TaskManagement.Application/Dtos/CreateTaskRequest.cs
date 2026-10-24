using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Dtos;

public sealed record CreateTaskRequest
{
    [Required]
    [StringLength(TaskItem.TitleMaxLength)]
    public string Title { get; init; } = string.Empty;

    [StringLength(TaskItem.DescriptionMaxLength)]
    public string? Description { get; init; }

    /// <summary>Defaults to Medium when omitted.</summary>
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;

    [StringLength(TaskItem.AssignedToMaxLength)]
    public string? AssignedTo { get; init; }
}
