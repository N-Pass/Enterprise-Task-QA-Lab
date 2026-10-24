using NUnit.Framework.Interfaces;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using TaskManagement.UITests.Pages;

namespace TaskManagement.UITests.Infrastructure;

/// <summary>
/// Opens a fresh Chrome instance for every test (no shared cookies, storage or page state)
/// and attaches a screenshot and the page HTML when a test fails.
/// </summary>
public abstract class UiTestBase
{
    protected IWebDriver Driver { get; private set; } = null!;

    [SetUp]
    public void StartBrowser()
    {
        var options = new ChromeOptions();
        // Headless by default (CI); set UI_TEST_HEADED=1 to watch the browser.
        if (Environment.GetEnvironmentVariable("UI_TEST_HEADED") != "1")
        {
            options.AddArgument("--headless=new");
        }
        options.AddArgument("--window-size=1280,900");
        options.AddArgument("--lang=en-US");
        options.AddArgument("--no-sandbox");            // required in many CI containers
        options.AddArgument("--disable-dev-shm-usage"); // avoids crashes with small /dev/shm in CI

        // Selenium Manager downloads a ChromeDriver matching the installed Chrome.
        Driver = new ChromeDriver(options);

        // Explicit waits only: an implicit wait would silently add delays to every lookup.
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
    }

    [TearDown]
    public void StopBrowser()
    {
        try
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                SaveFailureArtifacts();
            }
        }
        finally
        {
            Driver.Quit();
            Driver.Dispose();
        }
    }

    protected TaskListPage OpenTaskList() => TaskListPage.Open(Driver, UiTestEnvironment.BaseUrl);

    /// <summary>Unique title so each test works only with the data it created.</summary>
    protected static string UniqueTitle(string prefix) => $"{prefix} {Guid.NewGuid().ToString("N")[..8]}";

    private void SaveFailureArtifacts()
    {
        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ui-failures");
        Directory.CreateDirectory(directory);
        var baseName = string.Concat(TestContext.CurrentContext.Test.Name.Split(Path.GetInvalidFileNameChars()));

        var screenshotPath = Path.Combine(directory, $"{baseName}.png");
        ((ITakesScreenshot)Driver).GetScreenshot().SaveAsFile(screenshotPath);
        TestContext.AddTestAttachment(screenshotPath, "Screenshot at failure");

        var htmlPath = Path.Combine(directory, $"{baseName}.html");
        File.WriteAllText(htmlPath, Driver.PageSource);
        TestContext.AddTestAttachment(htmlPath, "Page HTML at failure");

        TestContext.Progress.WriteLine($"Failure artifacts saved to {directory}");
    }
}
