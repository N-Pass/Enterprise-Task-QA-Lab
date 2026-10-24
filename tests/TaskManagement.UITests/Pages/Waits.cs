using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace TaskManagement.UITests.Pages;

/// <summary>Explicit waits shared by the page objects. No fixed sleeps anywhere.</summary>
internal static class Waits
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    public static T Until<T>(IWebDriver driver, Func<IWebDriver, T> condition, string description)
    {
        var wait = new WebDriverWait(driver, DefaultTimeout) { Message = $"Timed out waiting for: {description}" };

        // The task list is re-rendered after every action, so elements found a moment ago
        // can become stale or briefly missing; keep polling instead of failing.
        wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException), typeof(NoSuchElementException));
        return wait.Until(condition);
    }

    public static By TestId(string id) => By.CssSelector($"[data-testid='{id}']");
}
