using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Abstractions;

/// <summary>
/// Persistence contract for tasks. Defined here and implemented in Infrastructure,
/// so the Application layer does not depend on Entity Framework Core.
/// </summary>
public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns tasks matching the optional filters, newest first.</summary>
    Task<IReadOnlyList<TaskItem>> ListAsync(
        TaskItemStatus? status,
        TaskPriority? priority,
        CancellationToken cancellationToken = default);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    void Remove(TaskItem task);

    /// <summary>Persists all pending changes (acts as the unit of work).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
