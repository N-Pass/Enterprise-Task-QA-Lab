using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TaskManagement.Application.Dtos;
using TaskManagement.Domain.Enums;
using TaskManagement.IntegrationTests.Infrastructure;

namespace TaskManagement.IntegrationTests;

[TestFixture]
public class TasksApiTests : ApiTestBase
{
    private static readonly Guid UnknownId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ---- Create ----

    [Test]
    public async Task Create_WithValidData_Returns201WithLocationAndTask()
    {
        var response = await Client.PostAsJsonAsync(TasksUrl, new
        {
            title = "Prepare sprint review",
            description = "Collect demo items.",
            priority = "High",
            assignedTo = "Jordan"
        }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var task = await response.Content.ReadFromJsonAsync<TaskResponse>(Json);

        Assert.Multiple(() =>
        {
            Assert.That(response.Headers.Location?.AbsolutePath, Is.EqualTo($"{TasksUrl}/{task!.Id}"));
            Assert.That(task.Title, Is.EqualTo("Prepare sprint review"));
            Assert.That(task.Description, Is.EqualTo("Collect demo items."));
            Assert.That(task.Priority, Is.EqualTo(TaskPriority.High));
            Assert.That(task.AssignedTo, Is.EqualTo("Jordan"));
            Assert.That(task.Status, Is.EqualTo(TaskItemStatus.ToDo), "New tasks start as ToDo.");
            Assert.That(task.CreatedAtUtc, Is.EqualTo(Factory.Clock.GetUtcNow().UtcDateTime));
            Assert.That(task.UpdatedAtUtc, Is.EqualTo(task.CreatedAtUtc));
        });
    }

    [Test]
    public async Task Create_WithoutPriority_DefaultsToMedium()
    {
        var task = await CreateTaskAsync(new { title = "No priority given" });

        Assert.That(task.Priority, Is.EqualTo(TaskPriority.Medium));
    }

    [Test]
    public async Task Create_ReturnsTimestampsInUtc()
    {
        var response = await Client.PostAsJsonAsync(TasksUrl, new { title = "UTC check" }, Json);
        var body = await ReadJsonAsync(response);

        // ISO 8601 with "Z" suffix means UTC on the wire.
        Assert.That(body.GetProperty("createdAtUtc").GetString(), Does.EndWith("Z"));
    }

    [TestCase(null, TestName = "Create_WithNullTitle_Returns400")]
    [TestCase("", TestName = "Create_WithEmptyTitle_Returns400")]
    [TestCase("   ", TestName = "Create_WithWhitespaceTitle_Returns400")]
    public async Task Create_WithMissingTitle_Returns400(string? title)
    {
        var response = await Client.PostAsJsonAsync(TasksUrl, new { title }, Json);

        await AssertValidationErrorAsync(response, "Title");
    }

    [Test]
    public async Task Create_WithTitleOver120Characters_Returns400()
    {
        var response = await Client.PostAsJsonAsync(TasksUrl, new { title = new string('a', 121) }, Json);

        await AssertValidationErrorAsync(response, "Title");
    }

    [Test]
    public async Task Create_WithTitleOfExactly120Characters_Returns201()
    {
        var response = await Client.PostAsJsonAsync(TasksUrl, new { title = new string('a', 120) }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task Create_WithDescriptionOver1000Characters_Returns400()
    {
        var response = await Client.PostAsJsonAsync(TasksUrl, new { title = "Valid", description = new string('d', 1001) }, Json);

        await AssertValidationErrorAsync(response, "Description");
    }

    [TestCase("\"Urgent\"", TestName = "Create_WithUnknownPriorityName_Returns400")]
    [TestCase("2", TestName = "Create_WithNumericPriority_Returns400")]
    public async Task Create_WithInvalidPriority_Returns400(string priorityJson)
    {
        var response = await PostRawJsonAsync(TasksUrl, $$"""{ "title": "Valid", "priority": {{priorityJson}} }""");

        await AssertValidationErrorAsync(response, "$.priority");
    }

    [Test]
    public async Task Create_WithMalformedJson_Returns400WithoutInternalDetails()
    {
        var response = await PostRawJsonAsync(TasksUrl, """{ "title": """);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var body = await response.Content.ReadAsStringAsync();
        Assert.That(body, Does.Not.Contain("TaskManagement."), "Internal type names must not leak.");
    }

    // ---- Read ----

    [Test]
    public async Task GetById_ExistingTask_Returns200WithTask()
    {
        var created = await CreateTaskAsync(new { title = "Find me", priority = "Low" });

        var response = await Client.GetAsync($"{TasksUrl}/{created.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadFromJsonAsync<TaskResponse>(Json), Is.EqualTo(created));
    }

    [Test]
    public async Task GetById_UnknownId_Returns404ProblemDetails()
    {
        var response = await Client.GetAsync($"{TasksUrl}/{UnknownId}");

        await AssertNotFoundAsync(response);
    }

    [Test]
    public async Task GetById_MalformedId_Returns404()
    {
        var response = await Client.GetAsync($"{TasksUrl}/not-a-guid");

        await AssertNotFoundAsync(response);
    }

    [Test]
    public async Task List_ReturnsAllTasksNewestFirst()
    {
        await CreateTaskAsync(new { title = "First" });
        Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await CreateTaskAsync(new { title = "Second" });

        var tasks = await Client.GetFromJsonAsync<List<TaskResponse>>(TasksUrl, Json);

        Assert.That(tasks!.Select(t => t.Title), Is.EqualTo(new[] { "Second", "First" }));
    }

    [Test]
    public async Task List_WithStatusAndPriorityFilters_ReturnsOnlyMatchingTasks()
    {
        var match = await CreateTaskAsync(new { title = "High in progress", priority = "High" });
        await Client.PatchAsJsonAsync($"{TasksUrl}/{match.Id}/status", new { status = "InProgress" }, Json);
        await CreateTaskAsync(new { title = "High to do", priority = "High" });
        var lowInProgress = await CreateTaskAsync(new { title = "Low in progress", priority = "Low" });
        await Client.PatchAsJsonAsync($"{TasksUrl}/{lowInProgress.Id}/status", new { status = "InProgress" }, Json);

        var tasks = await Client.GetFromJsonAsync<List<TaskResponse>>($"{TasksUrl}?status=InProgress&priority=High", Json);

        Assert.That(tasks!.Select(t => t.Title), Is.EqualTo(new[] { "High in progress" }));
    }

    [TestCase("Blocked")]
    [TestCase("7")]
    public async Task List_WithInvalidStatusFilter_Returns400(string status)
    {
        var response = await Client.GetAsync($"{TasksUrl}?status={status}");

        await AssertValidationErrorAsync(response, "status");
    }

    // ---- Update ----

    [Test]
    public async Task Update_WithValidData_Returns200AndRefreshesModificationTimestamp()
    {
        var created = await CreateTaskAsync(new { title = "Original", priority = "High", assignedTo = "Ana" });
        Factory.Clock.Advance(TimeSpan.FromHours(1));

        var response = await Client.PutAsJsonAsync($"{TasksUrl}/{created.Id}",
            new { title = "Updated", description = "New details", priority = "Low", assignedTo = "Bea" }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<TaskResponse>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(updated!.Title, Is.EqualTo("Updated"));
            Assert.That(updated.Description, Is.EqualTo("New details"));
            Assert.That(updated.Priority, Is.EqualTo(TaskPriority.Low));
            Assert.That(updated.AssignedTo, Is.EqualTo("Bea"));
            Assert.That(updated.CreatedAtUtc, Is.EqualTo(created.CreatedAtUtc));
            Assert.That(updated.UpdatedAtUtc, Is.EqualTo(created.UpdatedAtUtc.AddHours(1)));
        });
    }

    [Test]
    public async Task Update_IsPersisted()
    {
        var created = await CreateTaskAsync(new { title = "Before" });

        await Client.PutAsJsonAsync($"{TasksUrl}/{created.Id}", new { title = "After", priority = "Medium" }, Json);
        var reloaded = await Client.GetFromJsonAsync<TaskResponse>($"{TasksUrl}/{created.Id}", Json);

        Assert.That(reloaded!.Title, Is.EqualTo("After"));
    }

    [Test]
    public async Task Update_WithDifferentIdInBody_DoesNotChangeIdentifier()
    {
        var created = await CreateTaskAsync(new { title = "Keep my id" });

        var response = await Client.PutAsJsonAsync($"{TasksUrl}/{created.Id}",
            new { id = Guid.NewGuid(), title = "Still same id", priority = "Medium" }, Json);
        var updated = await response.Content.ReadFromJsonAsync<TaskResponse>(Json);

        Assert.That(updated!.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task Update_WithInvalidData_Returns400AndLeavesTaskUnchanged()
    {
        var created = await CreateTaskAsync(new { title = "Unchanged" });

        var response = await Client.PutAsJsonAsync($"{TasksUrl}/{created.Id}", new { title = "", priority = "Low" }, Json);
        var reloaded = await Client.GetFromJsonAsync<TaskResponse>($"{TasksUrl}/{created.Id}", Json);

        await AssertValidationErrorAsync(response, "Title");
        Assert.That(reloaded, Is.EqualTo(created));
    }

    [Test]
    public async Task Update_WithoutPriority_Returns400()
    {
        var created = await CreateTaskAsync(new { title = "Needs priority" });

        var response = await Client.PutAsJsonAsync($"{TasksUrl}/{created.Id}", new { title = "No priority" }, Json);

        await AssertValidationErrorAsync(response, "Priority");
    }

    [Test]
    public async Task Update_UnknownId_Returns404()
    {
        var response = await Client.PutAsJsonAsync($"{TasksUrl}/{UnknownId}", new { title = "Ghost", priority = "Low" }, Json);

        await AssertNotFoundAsync(response);
    }

    // ---- Status ----

    [Test]
    public async Task ChangeStatus_WithValidStatus_Returns200AndPersists()
    {
        var created = await CreateTaskAsync(new { title = "Move me" });
        Factory.Clock.Advance(TimeSpan.FromMinutes(5));

        var response = await Client.PatchAsJsonAsync($"{TasksUrl}/{created.Id}/status", new { status = "Done" }, Json);
        var reloaded = await Client.GetFromJsonAsync<TaskResponse>($"{TasksUrl}/{created.Id}", Json);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(reloaded!.Status, Is.EqualTo(TaskItemStatus.Done));
            Assert.That(reloaded.UpdatedAtUtc, Is.EqualTo(created.UpdatedAtUtc.AddMinutes(5)));
        });
    }

    [Test]
    public async Task ChangeStatus_WithoutStatus_Returns400()
    {
        var created = await CreateTaskAsync(new { title = "Missing status" });

        var response = await Client.PatchAsJsonAsync($"{TasksUrl}/{created.Id}/status", new { }, Json);

        await AssertValidationErrorAsync(response, "Status");
    }

    [Test]
    public async Task ChangeStatus_UnknownId_Returns404()
    {
        var response = await Client.PatchAsJsonAsync($"{TasksUrl}/{UnknownId}/status", new { status = "Done" }, Json);

        await AssertNotFoundAsync(response);
    }

    // ---- Delete ----

    [Test]
    public async Task Delete_ExistingTask_Returns204AndTaskIsGone()
    {
        var created = await CreateTaskAsync(new { title = "Delete me" });

        var response = await Client.DeleteAsync($"{TasksUrl}/{created.Id}");
        var afterDelete = await Client.GetAsync($"{TasksUrl}/{created.Id}");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            Assert.That(afterDelete.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        });
    }

    [Test]
    public async Task Delete_UnknownId_Returns404()
    {
        var response = await Client.DeleteAsync($"{TasksUrl}/{UnknownId}");

        await AssertNotFoundAsync(response);
    }

    // ---- Helpers ----

    private Task<HttpResponseMessage> PostRawJsonAsync(string url, string json) =>
        Client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

    private static async Task AssertValidationErrorAsync(HttpResponseMessage response, string field)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));

        var body = await ReadJsonAsync(response);
        Assert.That(body.TryGetProperty("errors", out var errors), Is.True, "ProblemDetails should contain 'errors'.");
        Assert.That(errors.TryGetProperty(field, out _), Is.True, $"Expected a validation error for '{field}'. Body: {body}");
    }

    private static async Task AssertNotFoundAsync(HttpResponseMessage response)
    {
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));

        var body = await ReadJsonAsync(response);
        Assert.That(body.GetProperty("status").GetInt32(), Is.EqualTo(404));
    }
}
