namespace Application.UseCases.Exceptions;

/// <summary>
/// Base exception for use case failures. Controllers map this to HTTP 500.
/// </summary>
public class UseCaseException : Exception
{
    public UseCaseException(string message) : base(message) { }

    public UseCaseException(string message, Exception inner) : base(message, inner) { }
}
