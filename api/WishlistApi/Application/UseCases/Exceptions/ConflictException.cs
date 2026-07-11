namespace Application.UseCases.Exceptions;

/// <summary>
/// Thrown when a use case detects a conflict (e.g., duplicate resource, concurrency).
/// Controllers map this to HTTP 409.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }

    public ConflictException(string message, Exception inner) : base(message, inner) { }
}
