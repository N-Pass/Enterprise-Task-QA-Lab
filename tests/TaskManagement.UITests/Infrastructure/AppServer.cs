using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace TaskManagement.UITests.Infrastructure;

/// <summary>
/// Starts the compiled API as a separate process on a free port with its own temporary
/// SQLite database, so UI tests exercise the real application end to end.
/// Set UI_TEST_BASE_URL to run against an already running instance instead.
/// </summary>
public sealed class AppServer : IDisposable
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(60);

    private readonly Process? _process;
    private readonly string? _databasePath;
    private readonly StringBuilder _output = new();

    private AppServer(Uri baseUrl, Process? process, string? databasePath)
    {
        BaseUrl = baseUrl;
        _process = process;
        _databasePath = databasePath;
    }

    public Uri BaseUrl { get; }

    /// <summary>Console output of the API process, useful when startup fails.</summary>
    public string Output
    {
        get { lock (_output) { return _output.ToString(); } }
    }

    public static async Task<AppServer> StartAsync()
    {
        var externalUrl = Environment.GetEnvironmentVariable("UI_TEST_BASE_URL");
        if (!string.IsNullOrWhiteSpace(externalUrl))
        {
            return new AppServer(new Uri(externalUrl), process: null, databasePath: null);
        }

        var apiProjectDir = Path.Combine(FindRepositoryRoot(), "src", "TaskManagement.Api");
        var apiDll = Path.Combine(apiProjectDir, "bin", BuildConfiguration(), "net10.0", "TaskManagement.Api.dll");
        if (!File.Exists(apiDll))
        {
            throw new FileNotFoundException("Build the solution before running the UI tests.", apiDll);
        }

        var port = GetFreePort();
        var baseUrl = new Uri($"http://127.0.0.1:{port}");
        var databasePath = Path.Combine(Path.GetTempPath(), $"task-ui-tests-{Guid.NewGuid():N}.db");

        var startInfo = new ProcessStartInfo("dotnet")
        {
            // The API project folder is the content root, so wwwroot and appsettings are found.
            WorkingDirectory = apiProjectDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(apiDll);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        startInfo.Environment["ASPNETCORE_URLS"] = baseUrl.ToString();
        startInfo.Environment["ConnectionStrings__TaskDb"] = $"Data Source={databasePath}";
        startInfo.Environment["Database__SeedSampleData"] = "false";

        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the API process.");
        var server = new AppServer(baseUrl, process, databasePath);
        process.OutputDataReceived += (_, e) => server.Append(e.Data);
        process.ErrorDataReceived += (_, e) => server.Append(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await server.WaitUntilReadyAsync();
        }
        catch
        {
            server.Dispose();
            throw;
        }

        return server;
    }

    private void Append(string? line)
    {
        if (line is null) return;
        lock (_output) { _output.AppendLine(line); }
    }

    // Polls the API until it answers, instead of sleeping for a fixed time.
    private async Task WaitUntilReadyAsync()
    {
        using var http = new HttpClient { BaseAddress = BaseUrl, Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow + StartupTimeout;

        while (DateTime.UtcNow < deadline)
        {
            if (_process!.HasExited)
            {
                throw new InvalidOperationException($"The API process exited with code {_process.ExitCode}.\n{Output}");
            }

            try
            {
                var response = await http.GetAsync("/api/tasks");
                if (response.StatusCode == HttpStatusCode.OK) return;
            }
            catch (HttpRequestException) { /* not listening yet */ }
            catch (TaskCanceledException) { /* request timed out */ }

            await Task.Delay(250);
        }

        throw new TimeoutException($"The API did not start within {StartupTimeout.TotalSeconds}s.\n{Output}");
    }

    public void Dispose()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(5000);
        }
        _process?.Dispose();

        if (_databasePath is not null)
        {
            foreach (var file in new[] { _databasePath, $"{_databasePath}-shm", $"{_databasePath}-wal" })
            {
                try { File.Delete(file); } catch (IOException) { /* best effort cleanup */ }
            }
        }
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EnterpriseTaskQaLab.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root (EnterpriseTaskQaLab.slnx) not found.");
    }

    // The API is built with the same configuration (Debug/Release) as the test project.
    private static string BuildConfiguration() =>
        typeof(AppServer).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";
}
