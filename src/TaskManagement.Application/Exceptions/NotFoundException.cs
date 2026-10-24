namespace TaskManagement.Application.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist.
/// The API layer translates it into an HTTP 404 response.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string resourceName, Guid id)
        : base($"{resourceName} with id '{id}' was not found.")
    {
    }
}
