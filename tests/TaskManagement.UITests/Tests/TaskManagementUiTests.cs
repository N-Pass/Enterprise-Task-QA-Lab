using TaskManagement.UITests.Infrastructure;
using TaskManagement.UITests.Pages;

namespace TaskManagement.UITests.Tests;

/// <summary>
/// End-to-end browser tests. Every test creates the data it needs with a unique title,
/// so tests are independent of each other and of execution order.
/// </summary>
[TestFixture]
public class TaskManagementUiTests : UiTestBase
{
    [Test]
    [Description("TC-UI-001: Create a task successfully.")]
    public void TC_UI_001_CreateTaskSuccessfully()
    {
        var page = OpenTaskList();
        var title = UniqueTitle("Prepare release notes");

        var form = page.ClickNewTask();
        Assert.That(form.Heading, Is.EqualTo("New task"));
        form.EnterTitle(title)
            .EnterDescription("Summarize the changes for version 1.0.")
            .SelectPriority("High")
            .EnterAssignee("Alice")
            .Save();

        var card = page.WaitForTask(title);
        Assert.Multiple(() =>
        {
            Assert.That(page.WaitForMessage($"Task \"{title}\" was created."), Is.Not.Empty);
            Assert.That(card.Status, Is.EqualTo("To Do"), "New tasks start as To Do.");
            Assert.That(card.Priority, Is.EqualTo("High priority"));
            Assert.That(card.Assignee, Is.EqualTo("Assigned to Alice"));
        });

        page.Reload();
        Assert.That(page.HasTask(title), Is.True, "The task should still exist after reloading the page.");
    }

    [Test]
    [Description("TC-UI-002: Reject an invalid task form.")]
    public void TC_UI_002_RejectInvalidTaskForm()
    {
        var page = OpenTaskList();
        var tasksBefore = page.TaskCount;

        var form = page.ClickNewTask();
        form.EnterTitle("   ").EnterDescription("A task without a real title.");
        form.ClickSave();

        Assert.Multiple(() =>
        {
            Assert.That(form.WaitForTitleError(), Is.EqualTo("Title is required."));
            Assert.That(form.IsTitleMarkedInvalid, Is.True, "The field should be marked aria-invalid.");
            Assert.That(form.IsOpen, Is.True, "The dialog should stay open so the user can fix the input.");
        });

        // The browser prevents typing more than the maximum title length.
        form.EnterTitle(new string('x', 130));
        Assert.That(form.TitleValue, Has.Length.EqualTo(120));

        form.Cancel();
        page.Reload();
        Assert.That(page.TaskCount, Is.EqualTo(tasksBefore), "No task should have been created.");
    }

    [Test]
    [Description("TC-UI-003: Update an existing task.")]
    public void TC_UI_003_UpdateExistingTask()
    {
        var page = OpenTaskList();
        var originalTitle = UniqueTitle("Draft roadmap");
        var updatedTitle = $"{originalTitle} (final)";
        CreateTask(page, originalTitle, priority: "Low");

        var form = page.WaitForTask(originalTitle).Edit();
        Assert.Multiple(() =>
        {
            Assert.That(form.Heading, Is.EqualTo("Edit task"));
            Assert.That(form.TitleValue, Is.EqualTo(originalTitle), "The form should be pre-filled.");
        });
        form.EnterTitle(updatedTitle).SelectPriority("High").EnterAssignee("Bruno").Save();

        page.WaitForMessage($"Task \"{updatedTitle}\" was updated.");
        page.WaitForTaskToDisappear(originalTitle);
        var card = page.WaitForTask(updatedTitle);
        Assert.Multiple(() =>
        {
            Assert.That(card.Priority, Is.EqualTo("High priority"));
            Assert.That(card.Assignee, Is.EqualTo("Assigned to Bruno"));
        });

        page.Reload();
        Assert.That(page.WaitForTask(updatedTitle).Priority, Is.EqualTo("High priority"), "The update should be persisted.");
    }

    [Test]
    [Description("TC-UI-004: Change task status.")]
    public void TC_UI_004_ChangeTaskStatus()
    {
        var page = OpenTaskList();
        var title = UniqueTitle("Review pull request");
        CreateTask(page, title);
        var card = page.WaitForTask(title);
        Assert.That(card.Status, Is.EqualTo("To Do"));

        card.ChangeStatus("In Progress");
        page.WaitForMessage($"Status of \"{title}\" changed to In Progress.");

        card.ChangeStatus("Done");
        page.WaitForMessage($"Status of \"{title}\" changed to Done.");

        page.Reload();
        Assert.That(page.WaitForTask(title).Status, Is.EqualTo("Done"), "The status should be persisted.");
    }

    [Test]
    [Description("TC-UI-005: Delete a task.")]
    public void TC_UI_005_DeleteTask()
    {
        var page = OpenTaskList();
        var title = UniqueTitle("Obsolete task");
        CreateTask(page, title);

        // Cancelling the confirmation must keep the task.
        var confirmation = page.WaitForTask(title).Delete();
        Assert.That(confirmation.Description, Does.Contain($"\"{title}\""));
        confirmation.Cancel();
        Assert.That(page.HasTask(title), Is.True, "Cancel should keep the task.");

        page.WaitForTask(title).Delete().Confirm();

        page.WaitForMessage($"Task \"{title}\" was deleted.");
        page.WaitForTaskToDisappear(title);
        page.Reload();
        Assert.That(page.HasTask(title), Is.False, "The deletion should be persisted.");
    }

    [Test]
    [Description("TC-UI-006: Filter tasks by status and priority.")]
    public void TC_UI_006_FilterTasksByStatusAndPriority()
    {
        var page = OpenTaskList();
        var inProgressHigh = UniqueTitle("Filter in progress high");
        var toDoLow = UniqueTitle("Filter to do low");
        CreateTask(page, inProgressHigh, priority: "High");
        CreateTask(page, toDoLow, priority: "Low");
        page.WaitForTask(inProgressHigh).ChangeStatus("In Progress");

        page.FilterByStatus("In Progress");
        page.WaitForTaskToDisappear(toDoLow);
        Assert.That(page.HasTask(inProgressHigh), Is.True);

        page.FilterByPriority("Low");
        page.WaitForTaskToDisappear(inProgressHigh);

        page.ClearFilters();
        page.WaitForTask(inProgressHigh);
        page.WaitForTask(toDoLow);
    }

    private static void CreateTask(TaskListPage page, string title, string priority = "Medium")
    {
        page.ClickNewTask().EnterTitle(title).SelectPriority(priority).Save();
        page.WaitForTask(title);
    }
}
