using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    // Tracked query: the returned entity can be modified and then saved.
    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Tasks.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> ListAsync(
        TaskItemStatus? status,
        TaskPriority? priority,
        CancellationToken cancellationToken = default)
    {
        // Read-only query, so change tracking is disabled for efficiency.
        IQueryable<TaskItem> query = _context.Tasks.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(t => t.Status == status);
        }

        if (priority is not null)
        {
            query = query.Where(t => t.Priority == priority);
        }

        return await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default) =>
        await _context.Tasks.AddAsync(task, cancellationToken);

    public void Remove(TaskItem task) => _context.Tasks.Remove(task);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
