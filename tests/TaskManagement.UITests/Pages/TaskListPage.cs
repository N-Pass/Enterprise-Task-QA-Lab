using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using static TaskManagement.UITests.Pages.Waits;

namespace TaskManagement.UITests.Pages;

/// <summary>The main page: task list, filters, "New task" button and messages.</summary>
public sealed class TaskListPage
{
    private readonly IWebDriver _driver;

    private TaskListPage(IWebDriver driver)
    {
        _driver = driver;
    }

    public static TaskListPage Open(IWebDriver driver, Uri baseUrl)
    {
        driver.Navigate().GoToUrl(baseUrl);
        var page = new TaskListPage(driver);
        page.WaitUntilLoaded();
        return page;
    }

    /// <summary>Reloads the browser page, e.g. to prove that a change was persisted.</summary>
    public TaskListPage Reload()
    {
        _driver.Navigate().Refresh();
        WaitUntilLoaded();
        return this;
    }

    public TaskFormDialog ClickNewTask()
    {
        _driver.FindElement(TestId("new-task-button")).Click();
        return TaskFormDialog.WaitUntilOpen(_driver);
    }

    /// <summary>Waits until a card with this exact title is shown and returns it.</summary>
    public TaskCard WaitForTask(string title)
    {
        var id = Until(_driver, d => FindCardId(d, title), $"task \"{title}\" to appear in the list");
        return new TaskCard(_driver, id!);
    }

    public void WaitForTaskToDisappear(string title) =>
        Until(_driver, d => FindCardId(d, title) is null, $"task \"{title}\" to disappear from the list");

    public bool HasTask(string title) => FindCardId(_driver, title) is not null;

    public int TaskCount => _driver.FindElements(TestId("task-item")).Count;

    /// <summary>Waits for a success/error message containing the given text and returns the full message.</summary>
    public string WaitForMessage(string expectedText) =>
        // null means "not yet"; Until only returns once the condition yields a value.
        Until(_driver, d =>
        {
            var message = d.FindElement(TestId("message"));
            return message.Displayed && message.Text.Contains(expectedText, StringComparison.Ordinal) ? message.Text : null;
        }, $"message containing \"{expectedText}\"")!;

    public void FilterByStatus(string visibleText) =>
        new SelectElement(_driver.FindElement(TestId("filter-status"))).SelectByText(visibleText);

    public void FilterByPriority(string visibleText) =>
        new SelectElement(_driver.FindElement(TestId("filter-priority"))).SelectByText(visibleText);

    public void ClearFilters() => _driver.FindElement(TestId("clear-filters")).Click();

    private void WaitUntilLoaded() =>
        Until(_driver, d =>
            d.FindElement(TestId("task-list")).GetDomAttribute("aria-busy") == "false"
            && d.FindElement(TestId("task-count")).Text.Length > 0,
            "task list to finish loading");

    private static string? FindCardId(IWebDriver driver, string title) =>
        driver.FindElements(TestId("task-item"))
            .FirstOrDefault(card => card.FindElement(TestId("task-title")).Text == title)
            ?.GetDomAttribute("data-task-id");
}
