using TaskManagement.Application.Dtos;
using TaskManagement.Application.Exceptions;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;
using TaskManagement.UnitTests.Fakes;

namespace TaskManagement.UnitTests.Application;

[TestFixture]
public class TaskServiceTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    private InMemoryTaskRepository _repository = null!;
    private FixedTimeProvider _clock = null!;
    private TaskService _service = null!;

    // NUnit creates one fixture instance and runs [SetUp] before each test,
    // so every test starts with a fresh repository and clock.
    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryTaskRepository();
        _clock = new FixedTimeProvider(StartTime);
        _service = new TaskService(_repository, _clock);
    }

    private Task<TaskResponse> CreateSampleAsync() =>
        _service.CreateAsync(new CreateTaskRequest
        {
            Title = "Write test plan",
            Description = "Cover API and UI",
            Priority = TaskPriority.High,
            AssignedTo = "Carla"
        });

    [Test]
    public async Task CreateAsync_WithValidRequest_PersistsTaskAndReturnsIt()
    {
        var response = await CreateSampleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(_repository.Tasks, Has.Count.EqualTo(1));
            Assert.That(_repository.SaveChangesCallCount, Is.EqualTo(1));
            Assert.That(response.Id, Is.EqualTo(_repository.Tasks[0].Id));
            Assert.That(response.Title, Is.EqualTo("Write test plan"));
            Assert.That(response.Status, Is.EqualTo(TaskItemStatus.ToDo));
            Assert.That(response.CreatedAtUtc, Is.EqualTo(StartTime.UtcDateTime));
            Assert.That(response.CreatedAtUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        });
    }

    [Test]
    public async Task CreateAsync_WithoutPriority_DefaultsToMedium()
    {
        var response = await _service.CreateAsync(new CreateTaskRequest { Title = "No priority given" });

        Assert.That(response.Priority, Is.EqualTo(TaskPriority.Medium));
    }

    [Test]
    public void CreateAsync_WithInvalidTitle_ThrowsAndPersistsNothing()
    {
        Assert.ThrowsAsync<DomainValidationException>(
            () => _service.CreateAsync(new CreateTaskRequest { Title = "" }));

        Assert.That(_repository.SaveChangesCallCount, Is.Zero);
    }

    [Test]
    public async Task GetByIdAsync_ExistingTask_ReturnsIt()
    {
        var created = await CreateSampleAsync();

        var found = await _service.GetByIdAsync(created.Id);

        Assert.That(found, Is.EqualTo(created));
    }

    [Test]
    public void GetByIdAsync_UnknownId_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task UpdateAsync_ExistingTask_UpdatesFieldsAndModificationTimestamp()
    {
        var created = await CreateSampleAsync();
        _clock.Advance(TimeSpan.FromHours(2));

        var updated = await _service.UpdateAsync(created.Id, new UpdateTaskRequest
        {
            Title = "Write and review test plan",
            Description = null,
            Priority = TaskPriority.Low,
            AssignedTo = "Diego"
        });

        Assert.Multiple(() =>
        {
            Assert.That(updated.Id, Is.EqualTo(created.Id));
            Assert.That(updated.Title, Is.EqualTo("Write and review test plan"));
            Assert.That(updated.Description, Is.Null);
            Assert.That(updated.Priority, Is.EqualTo(TaskPriority.Low));
            Assert.That(updated.AssignedTo, Is.EqualTo("Diego"));
            Assert.That(updated.CreatedAtUtc, Is.EqualTo(created.CreatedAtUtc));
            Assert.That(updated.UpdatedAtUtc, Is.EqualTo(StartTime.AddHours(2).UtcDateTime));
            Assert.That(_repository.SaveChangesCallCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void UpdateAsync_UnknownId_ThrowsNotFound()
    {
        var request = new UpdateTaskRequest { Title = "Anything", Priority = TaskPriority.Low };

        Assert.ThrowsAsync<NotFoundException>(() => _service.UpdateAsync(Guid.NewGuid(), request));
    }

    [Test]
    public async Task ChangeStatusAsync_ExistingTask_ChangesStatusAndTimestamp()
    {
        var created = await CreateSampleAsync();
        _clock.Advance(TimeSpan.FromMinutes(30));

        var changed = await _service.ChangeStatusAsync(
            created.Id, new ChangeTaskStatusRequest { Status = TaskItemStatus.InProgress });

        Assert.Multiple(() =>
        {
            Assert.That(changed.Status, Is.EqualTo(TaskItemStatus.InProgress));
            Assert.That(changed.UpdatedAtUtc, Is.EqualTo(StartTime.AddMinutes(30).UtcDateTime));
        });
    }

    [Test]
    public void ChangeStatusAsync_UnknownId_ThrowsNotFound()
    {
        var request = new ChangeTaskStatusRequest { Status = TaskItemStatus.Done };

        Assert.ThrowsAsync<NotFoundException>(() => _service.ChangeStatusAsync(Guid.NewGuid(), request));
    }

    [Test]
    public async Task DeleteAsync_ExistingTask_RemovesIt()
    {
        var created = await CreateSampleAsync();

        await _service.DeleteAsync(created.Id);

        Assert.Multiple(() =>
        {
            Assert.That(_repository.Tasks, Is.Empty);
            Assert.That(_repository.SaveChangesCallCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void DeleteAsync_UnknownId_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task ListAsync_WithFilters_ReturnsOnlyMatchingTasks()
    {
        await _service.CreateAsync(new CreateTaskRequest { Title = "High todo", Priority = TaskPriority.High });
        await _service.CreateAsync(new CreateTaskRequest { Title = "Low todo", Priority = TaskPriority.Low });
        var inProgress = await _service.CreateAsync(new CreateTaskRequest { Title = "High doing", Priority = TaskPriority.High });
        await _service.ChangeStatusAsync(inProgress.Id, new ChangeTaskStatusRequest { Status = TaskItemStatus.InProgress });

        var result = await _service.ListAsync(TaskItemStatus.ToDo, TaskPriority.High);

        Assert.That(result.Select(t => t.Title), Is.EqualTo(new[] { "High todo" }));
    }
}
