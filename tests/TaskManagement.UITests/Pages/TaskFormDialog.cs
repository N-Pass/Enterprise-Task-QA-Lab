using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using static TaskManagement.UITests.Pages.Waits;

namespace TaskManagement.UITests.Pages;

/// <summary>The create/edit task dialog.</summary>
public sealed class TaskFormDialog
{
    private readonly IWebDriver _driver;

    private TaskFormDialog(IWebDriver driver)
    {
        _driver = driver;
    }

    public static TaskFormDialog WaitUntilOpen(IWebDriver driver)
    {
        Until(driver, d => IsDialogOpen(d), "task form dialog to open");
        return new TaskFormDialog(driver);
    }

    public string Heading => _driver.FindElement(TestId("task-dialog-title")).Text;
    public bool IsOpen => IsDialogOpen(_driver);
    public string TitleValue => Field("input-title").GetDomProperty("value") ?? string.Empty;

    public TaskFormDialog EnterTitle(string title) => Type("input-title", title);
    public TaskFormDialog EnterDescription(string description) => Type("input-description", description);
    public TaskFormDialog EnterAssignee(string assignee) => Type("input-assignee", assignee);

    public TaskFormDialog SelectPriority(string visibleText)
    {
        new SelectElement(Field("input-priority")).SelectByText(visibleText);
        return this;
    }

    /// <summary>Saves and waits for the dialog to close (the expected outcome for valid data).</summary>
    public void Save()
    {
        ClickSave();
        Until(_driver, d => !IsDialogOpen(d), "task form dialog to close after saving");
    }

    /// <summary>Clicks save without expecting success, e.g. to trigger validation.</summary>
    public void ClickSave() => _driver.FindElement(TestId("save-button")).Click();

    public void Cancel()
    {
        _driver.FindElement(TestId("cancel-button")).Click();
        Until(_driver, d => !IsDialogOpen(d), "task form dialog to close after cancel");
    }

    /// <summary>Waits for the validation message under the Title field.</summary>
    public string WaitForTitleError() =>
        // null means "not yet"; Until only returns once the condition yields a value.
        Until(_driver, d =>
        {
            var error = d.FindElement(TestId("error-title"));
            return error.Displayed ? error.Text : null;
        }, "title validation message")!;

    public bool IsTitleMarkedInvalid => Field("input-title").GetDomAttribute("aria-invalid") == "true";

    private TaskFormDialog Type(string testId, string text)
    {
        var field = Field(testId);
        field.Clear();
        field.SendKeys(text);
        return this;
    }

    private IWebElement Field(string testId) => _driver.FindElement(TestId(testId));

    private static bool IsDialogOpen(IWebDriver driver) =>
        driver.FindElement(TestId("task-dialog")).GetDomAttribute("open") is not null;
}
