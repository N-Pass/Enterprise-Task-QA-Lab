using TaskManagement.UITests.Infrastructure;

namespace TaskManagement.UITests;

/// <summary>
/// Runs once for the whole UI test assembly: starts the API before the first test
/// and stops it (deleting its temporary database) after the last one.
/// It lives in the root namespace on purpose: NUnit applies a [SetUpFixture] only to
/// tests in its own namespace and below.
/// </summary>
[SetUpFixture]
public sealed class UiTestEnvironment
{
    private static AppServer? _server;

    public static Uri BaseUrl => _server?.BaseUrl ?? throw new InvalidOperationException("The app server is not running.");

    [OneTimeSetUp]
    public async Task StartServer()
    {
        _server = await AppServer.StartAsync();
        TestContext.Progress.WriteLine($"UI tests running against {_server.BaseUrl}");
    }

    [OneTimeTearDown]
    public void StopServer()
    {
        _server?.Dispose();
    }
}
