using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManagement.Application.Dtos;

namespace TaskManagement.IntegrationTests.Infrastructure;

/// <summary>
/// Creates a fresh API host, database and HTTP client before every test,
/// so tests never share data and can run in any order.
/// </summary>
public abstract class ApiTestBase
{
    protected const string TasksUrl = "/api/tasks";

    // Same conventions as the API: camelCase properties and enums as strings.
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected TaskApiFactory Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    [SetUp]
    public void CreateHost()
    {
        Factory = new TaskApiFactory();
        Client = Factory.CreateClient();
    }

    [TearDown]
    public void DisposeHost()
    {
        Client.Dispose();
        Factory.Dispose();
    }

    /// <summary>Creates a task through the API and returns the response body.</summary>
    protected async Task<TaskResponse> CreateTaskAsync(object request)
    {
        var response = await Client.PostAsJsonAsync(TasksUrl, request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Json);
}
