namespace TaskManagement.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would leave a domain object in an invalid state.
/// The API layer translates it into an HTTP 400 validation response.
/// </summary>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException(string propertyName, string message)
        : base(message)
    {
        PropertyName = propertyName;
    }

    /// <summary>Name of the property that failed validation, e.g. "Title".</summary>
    public string PropertyName { get; }
}
