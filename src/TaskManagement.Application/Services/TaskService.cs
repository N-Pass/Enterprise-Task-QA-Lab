using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Dtos;
using TaskManagement.Application.Exceptions;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services;

/// <summary>
/// Orchestrates task use cases: loads entities, delegates business rules to the
/// domain, persists changes and maps results to response DTOs.
/// </summary>
public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly TimeProvider _timeProvider;

    public TaskService(ITaskRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<TaskResponse>> ListAsync(
        TaskItemStatus? status,
        TaskPriority? priority,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _repository.ListAsync(status, priority, cancellationToken);
        return tasks.Select(TaskResponse.FromEntity).ToList();
    }

    public async Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetExistingAsync(id, cancellationToken);
        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = new TaskItem(request.Title, request.Description, request.Priority, request.AssignedTo, UtcNow());

        await _repository.AddAsync(task, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = await GetExistingAsync(id, cancellationToken);

        // Priority is guaranteed by [Required]; the fallback only guards direct callers.
        task.Update(request.Title, request.Description, request.Priority ?? task.Priority, request.AssignedTo, UtcNow());
        await _repository.SaveChangesAsync(cancellationToken);

        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> ChangeStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken = default)
    {
        var task = await GetExistingAsync(id, cancellationToken);

        task.ChangeStatus(request.Status ?? task.Status, UtcNow());
        await _repository.SaveChangesAsync(cancellationToken);

        return TaskResponse.FromEntity(task);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetExistingAsync(id, cancellationToken);

        _repository.Remove(task);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<TaskItem> GetExistingAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Task", id);
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;
}
