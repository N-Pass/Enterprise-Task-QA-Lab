using System.ComponentModel.DataAnnotations;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Dtos;

public sealed record ChangeTaskStatusRequest
{
    // Nullable so that a missing value is reported instead of silently becoming ToDo.
    [Required]
    public TaskItemStatus? Status { get; init; }
}
