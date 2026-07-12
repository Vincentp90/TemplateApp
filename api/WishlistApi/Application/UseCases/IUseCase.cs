namespace Application.UseCases;

/// <summary>
/// Common interface for all use cases. Each use case encapsulates a single
/// controller action's business logic.
/// </summary>
public interface IUseCase<TRequest, TResponse>
{
    Task<TResponse> ExecuteAsync(TRequest request);
}
