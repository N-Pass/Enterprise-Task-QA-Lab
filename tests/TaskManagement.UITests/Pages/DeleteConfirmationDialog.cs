using OpenQA.Selenium;
using static TaskManagement.UITests.Pages.Waits;

namespace TaskManagement.UITests.Pages;

/// <summary>The "Delete task?" confirmation dialog.</summary>
public sealed class DeleteConfirmationDialog
{
    private readonly IWebDriver _driver;

    private DeleteConfirmationDialog(IWebDriver driver)
    {
        _driver = driver;
    }

    public static DeleteConfirmationDialog WaitUntilOpen(IWebDriver driver)
    {
        Until(driver, d => IsDialogOpen(d), "delete confirmation dialog to open");
        return new DeleteConfirmationDialog(driver);
    }

    public string Description => _driver.FindElement(TestId("delete-description")).Text;

    public void Confirm() => ClickAndWaitForClose("confirm-delete-button");

    public void Cancel() => ClickAndWaitForClose("delete-cancel-button");

    private void ClickAndWaitForClose(string testId)
    {
        _driver.FindElement(TestId(testId)).Click();
        Until(_driver, d => !IsDialogOpen(d), "delete confirmation dialog to close");
    }

    private static bool IsDialogOpen(IWebDriver driver) =>
        driver.FindElement(TestId("delete-dialog")).GetDomAttribute("open") is not null;
}
