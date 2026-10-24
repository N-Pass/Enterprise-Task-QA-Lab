using TaskManagement.Application.Abstractions;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.UnitTests.Fakes;

/// <summary>Hand-written repository fake that keeps tasks in a list and counts saves.</summary>
internal sealed class InMemoryTaskRepository : ITaskRepository
{
    private readonly List<TaskItem> _tasks = [];

    public IReadOnlyList<TaskItem> Tasks => _tasks;

    public int SaveChangesCallCount { get; private set; }

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tasks.SingleOrDefault(t => t.Id == id));

    public Task<IReadOnlyList<TaskItem>> ListAsync(
        TaskItemStatus? status,
        TaskPriority? priority,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TaskItem> result = _tasks
            .Where(t => status is null || t.Status == status)
            .Where(t => priority is null || t.Priority == priority)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToList();

        return Task.FromResult(result);
    }

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _tasks.Add(task);
        return Task.CompletedTask;
    }

    public void Remove(TaskItem task) => _tasks.Remove(task);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
