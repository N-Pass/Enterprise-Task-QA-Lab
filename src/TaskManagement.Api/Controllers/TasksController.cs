using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Dtos;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Api.Controllers;

/// <summary>Manages tasks. Controllers only translate HTTP to service calls; rules live in the domain.</summary>
// Content types are declared per response instead of with a class-level [Produces]:
// [Produces("application/json")] would also force it onto error responses, which must be
// "application/problem+json" (RFC 9457).
[ApiController]
[Route("api/tasks")]
public sealed class TasksController : ControllerBase
{
    private const string Json = "application/json";
    private const string ProblemJson = "application/problem+json";

    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>Lists tasks, newest first, optionally filtered by status and/or priority.</summary>
    /// <param name="status">Optional status filter: ToDo, InProgress or Done.</param>
    /// <param name="priority">Optional priority filter: Low, Medium or High.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TaskResponse>>(StatusCodes.Status200OK, Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> List(
        [FromQuery] TaskItemStatus? status,
        [FromQuery] TaskPriority? priority,
        CancellationToken cancellationToken)
    {
        // Query-string enums also accept numbers (e.g. ?status=7); reject undefined values explicitly.
        if (status is not null && !Enum.IsDefined(status.Value))
        {
            ModelState.AddModelError(nameof(status), $"'{status}' is not a valid status.");
        }

        if (priority is not null && !Enum.IsDefined(priority.Value))
        {
            ModelState.AddModelError(nameof(priority), $"'{priority}' is not a valid priority.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        return Ok(await _taskService.ListAsync(status, priority, cancellationToken));
    }

    /// <summary>Gets a single task by its identifier.</summary>
    /// <param name="id">Task identifier (GUID).</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK, Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<ActionResult<TaskResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>Creates a task. New tasks always start with status ToDo.</summary>
    /// <param name="request">Task details. Priority defaults to Medium when omitted.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created, Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        var created = await _taskService.CreateAsync(request, cancellationToken);

        // 201 with a Location header pointing to the new resource.
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Replaces the editable details of a task. Status is changed via PATCH /status.</summary>
    /// <param name="id">Task identifier (GUID). Taken from the URL only; it cannot be changed.</param>
    /// <param name="request">New task details.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK, Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<ActionResult<TaskResponse>> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.UpdateAsync(id, request, cancellationToken));
    }

    /// <summary>Changes the status of a task.</summary>
    /// <param name="id">Task identifier (GUID).</param>
    /// <param name="request">The new status.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    [HttpPatch("{id:guid}/status")]
    [Consumes("application/json")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK, Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<ActionResult<TaskResponse>> ChangeStatus(Guid id, ChangeTaskStatusRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.ChangeStatusAsync(id, request, cancellationToken));
    }

    /// <summary>Deletes a task.</summary>
    /// <param name="id">Task identifier (GUID).</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, ProblemJson)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _taskService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
