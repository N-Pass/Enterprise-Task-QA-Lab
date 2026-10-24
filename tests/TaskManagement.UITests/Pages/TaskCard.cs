using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using static TaskManagement.UITests.Pages.Waits;

namespace TaskManagement.UITests.Pages;

/// <summary>
/// One task in the list. It stores the task id, not an IWebElement: the list is re-rendered
/// after each action, so the card is looked up again every time to avoid stale elements.
/// </summary>
public sealed class TaskCard
{
    private readonly IWebDriver _driver;

    public TaskCard(IWebDriver driver, string taskId)
    {
        _driver = driver;
        TaskId = taskId;
    }

    public string TaskId { get; }

    public string Title => Part("task-title").Text;
    public string Status => Part("task-status-label").Text;
    public string Priority => Part("task-priority").Text;
    public string Assignee => Part("task-assignee").Text;

    public TaskFormDialog Edit()
    {
        Part("edit-button").Click();
        return TaskFormDialog.WaitUntilOpen(_driver);
    }

    public DeleteConfirmationDialog Delete()
    {
        Part("delete-button").Click();
        return DeleteConfirmationDialog.WaitUntilOpen(_driver);
    }

    /// <summary>Selects a new status and waits until the re-rendered card shows it.</summary>
    public void ChangeStatus(string visibleText)
    {
        new SelectElement(Part("status-select")).SelectByText(visibleText);
        Until(_driver, _ => Status == visibleText, $"task status to become \"{visibleText}\"");
    }

    private IWebElement Part(string testId) =>
        _driver.FindElement(By.CssSelector($"[data-task-id='{TaskId}'] [data-testid='{testId}']"));
}
