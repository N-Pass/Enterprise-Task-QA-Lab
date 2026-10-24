using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;

namespace TaskManagement.UnitTests.Domain;

[TestFixture]
public class TaskItemTests
{
    private static readonly DateTime CreatedAt = new(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime LaterAt = new(2026, 1, 10, 15, 30, 0, DateTimeKind.Utc);

    private static TaskItem CreateValidTask() =>
        new("Prepare release notes", "Summarize changes for v1.0", TaskPriority.High, "Alice", CreatedAt);

    // ---- Creation ----

    [Test]
    public void Constructor_WithValidData_StartsWithToDoStatus()
    {
        var task = CreateValidTask();

        Assert.That(task.Status, Is.EqualTo(TaskItemStatus.ToDo));
    }

    [Test]
    public void Constructor_WithValidData_SetsAllProperties()
    {
        var task = CreateValidTask();

        Assert.Multiple(() =>
        {
            Assert.That(task.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(task.Title, Is.EqualTo("Prepare release notes"));
            Assert.That(task.Description, Is.EqualTo("Summarize changes for v1.0"));
            Assert.That(task.Priority, Is.EqualTo(TaskPriority.High));
            Assert.That(task.AssignedTo, Is.EqualTo("Alice"));
            Assert.That(task.CreatedAtUtc, Is.EqualTo(CreatedAt));
            Assert.That(task.UpdatedAtUtc, Is.EqualTo(CreatedAt));
        });
    }

    [Test]
    public void Constructor_CalledTwice_GeneratesUniqueIds()
    {
        var first = CreateValidTask();
        var second = CreateValidTask();

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public void Constructor_TrimsTextAndConvertsBlankOptionalFieldsToNull()
    {
        var task = new TaskItem("  Review PR  ", "   ", TaskPriority.Low, "", CreatedAt);

        Assert.Multiple(() =>
        {
            Assert.That(task.Title, Is.EqualTo("Review PR"));
            Assert.That(task.Description, Is.Null);
            Assert.That(task.AssignedTo, Is.Null);
        });
    }

    // ---- Title rules ----

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_WithMissingTitle_Throws(string? title)
    {
        var ex = Assert.Throws<DomainValidationException>(
            () => new TaskItem(title!, null, TaskPriority.Medium, null, CreatedAt));

        Assert.That(ex!.PropertyName, Is.EqualTo(nameof(TaskItem.Title)));
    }

    [Test]
    public void Constructor_WithTitleAtMaxLength_Succeeds()
    {
        var title = new string('a', TaskItem.TitleMaxLength);

        var task = new TaskItem(title, null, TaskPriority.Medium, null, CreatedAt);

        Assert.That(task.Title, Has.Length.EqualTo(120));
    }

    [Test]
    public void Constructor_WithTitleOverMaxLength_Throws()
    {
        var title = new string('a', TaskItem.TitleMaxLength + 1);

        var ex = Assert.Throws<DomainValidationException>(
            () => new TaskItem(title, null, TaskPriority.Medium, null, CreatedAt));

        Assert.That(ex!.PropertyName, Is.EqualTo(nameof(TaskItem.Title)));
    }

    // ---- Other field rules ----

    [Test]
    public void Constructor_WithDescriptionAtMaxLength_Succeeds()
    {
        var description = new string('d', TaskItem.DescriptionMaxLength);

        var task = new TaskItem("Title", description, TaskPriority.Medium, null, CreatedAt);

        Assert.That(task.Description, Has.Length.EqualTo(1000));
    }

    [Test]
    public void Constructor_WithDescriptionOverMaxLength_Throws()
    {
        var description = new string('d', TaskItem.DescriptionMaxLength + 1);

        var ex = Assert.Throws<DomainValidationException>(
            () => new TaskItem("Title", description, TaskPriority.Medium, null, CreatedAt));

        Assert.That(ex!.PropertyName, Is.EqualTo(nameof(TaskItem.Description)));
    }

    [Test]
    public void Constructor_WithAssignedToOverMaxLength_Throws()
    {
        var assignedTo = new string('n', TaskItem.AssignedToMaxLength + 1);

        var ex = Assert.Throws<DomainValidationException>(
            () => new TaskItem("Title", null, TaskPriority.Medium, assignedTo, CreatedAt));

        Assert.That(ex!.PropertyName, Is.EqualTo(nameof(TaskItem.AssignedTo)));
    }

    [Test]
    public void Constructor_WithUndefinedPriority_Throws()
    {
        var ex = Assert.Throws<DomainValidationException>(
            () => new TaskItem("Title", null, (TaskPriority)99, null, CreatedAt));

        Assert.That(ex!.PropertyName, Is.EqualTo(nameof(TaskItem.Priority)));
    }

    [Test]
    public void Constructor_WithNonUtcTimestamp_Throws()
    {
        var localTime = new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () => new TaskItem("Title", null, TaskPriority.Medium, null, localTime));
    }

    // ---- Update ----

    [Test]
    public void Update_WithValidData_ChangesFieldsAndModificationTimestamp()
    {
        var task = CreateValidTask();
        var originalId = task.Id;

        task.Update("Publish release notes", "Post on the wiki", TaskPriority.Low, "Bob", LaterAt);

        Assert.Multiple(() =>
        {
            Assert.That(task.Title, Is.EqualTo("Publish release notes"));
            Assert.That(task.Description, Is.EqualTo("Post on the wiki"));
            Assert.That(task.Priority, Is.EqualTo(TaskPriority.Low));
            Assert.That(task.AssignedTo, Is.EqualTo("Bob"));
            Assert.That(task.UpdatedAtUtc, Is.EqualTo(LaterAt));
            Assert.That(task.CreatedAtUtc, Is.EqualTo(CreatedAt), "Creation date must not change.");
            Assert.That(task.Id, Is.EqualTo(originalId), "Identifier must not change.");
        });
    }

    [Test]
    public void Update_DoesNotChangeStatus()
    {
        var task = CreateValidTask();
        task.ChangeStatus(TaskItemStatus.InProgress, LaterAt);

        task.Update("New title", null, TaskPriority.Medium, null, LaterAt.AddHours(1));

        Assert.That(task.Status, Is.EqualTo(TaskItemStatus.InProgress));
    }

    [Test]
    public void Update_WithInvalidData_ThrowsAndLeavesTaskUnchanged()
    {
        var task = CreateValidTask();
        var tooLongDescription = new string('d', TaskItem.DescriptionMaxLength + 1);

        Assert.Throws<DomainValidationException>(
            () => task.Update("Valid new title", tooLongDescription, TaskPriority.Low, "Bob", LaterAt));

        Assert.Multiple(() =>
        {
            Assert.That(task.Title, Is.EqualTo("Prepare release notes"));
            Assert.That(task.Priority, Is.EqualTo(TaskPriority.High));
            Assert.That(task.UpdatedAtUtc, Is.EqualTo(CreatedAt));
        });
    }

    // ---- Status changes ----

    [TestCase(TaskItemStatus.ToDo, TaskItemStatus.InProgress)]
    [TestCase(TaskItemStatus.ToDo, TaskItemStatus.Done)]
    [TestCase(TaskItemStatus.InProgress, TaskItemStatus.Done)]
    [TestCase(TaskItemStatus.InProgress, TaskItemStatus.ToDo)]
    [TestCase(TaskItemStatus.Done, TaskItemStatus.InProgress)]
    [TestCase(TaskItemStatus.Done, TaskItemStatus.ToDo)]
    public void ChangeStatus_ToDifferentStatus_UpdatesStatusAndTimestamp(TaskItemStatus from, TaskItemStatus to)
    {
        var task = CreateValidTask();
        var arrangeTime = CreatedAt.AddMinutes(1);
        task.ChangeStatus(from, arrangeTime);

        task.ChangeStatus(to, LaterAt);

        Assert.Multiple(() =>
        {
            Assert.That(task.Status, Is.EqualTo(to));
            Assert.That(task.UpdatedAtUtc, Is.EqualTo(LaterAt));
        });
    }

    [Test]
    public void ChangeStatus_ToSameStatus_IsNoOp()
    {
        var task = CreateValidTask();

        task.ChangeStatus(TaskItemStatus.ToDo, LaterAt);

        Assert.Multiple(() =>
        {
            Assert.That(task.Status, Is.EqualTo(TaskItemStatus.ToDo));
            Assert.That(task.UpdatedAtUtc, Is.EqualTo(CreatedAt));
        });
    }

    [Test]
    public void ChangeStatus_ToUndefinedStatus_Throws()
    {
        var task = CreateValidTask();

        var ex = Assert.Throws<DomainValidationException>(
            () => task.ChangeStatus((TaskItemStatus)42, LaterAt));

        Assert.That(ex!.PropertyName, Is.EqualTo(nameof(TaskItem.Status)));
    }
}
