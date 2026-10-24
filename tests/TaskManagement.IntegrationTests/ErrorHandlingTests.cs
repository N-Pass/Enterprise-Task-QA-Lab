using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Dtos;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Enums;
using TaskManagement.IntegrationTests.Infrastructure;

namespace TaskManagement.IntegrationTests;

[TestFixture]
public class ErrorHandlingTests : ApiTestBase
{
    private const string SecretMessage = "Connection to db-server-01 failed: password=hunter2";

    [Test]
    public async Task UnexpectedException_Returns500WithoutSensitiveDetails()
    {
        // Swap the real service for one that fails, simulating an unexpected infrastructure error.
        using var client = Factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddScoped<ITaskService, ThrowingTaskService>()))
            .CreateClient();

        var response = await client.GetAsync(TasksUrl);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
            Assert.That(body, Does.Contain("An unexpected error occurred."));
            Assert.That(body, Does.Not.Contain(SecretMessage), "The exception message must not be returned.");
            Assert.That(body, Does.Not.Contain("InvalidOperationException"), "The exception type must not be returned.");
            Assert.That(body, Does.Not.Contain(" at "), "A stack trace must not be returned.");
        });
    }

    private sealed class ThrowingTaskService : ITaskService
    {
        private static Exception Fail() => new InvalidOperationException(SecretMessage);

        public Task<IReadOnlyList<TaskResponse>> ListAsync(TaskItemStatus? status, TaskPriority? priority, CancellationToken cancellationToken = default) => throw Fail();
        public Task<TaskResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw Fail();
        public Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default) => throw Fail();
        public Task<TaskResponse> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken = default) => throw Fail();
        public Task<TaskResponse> ChangeStatusAsync(Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken = default) => throw Fail();
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw Fail();
    }
}
