namespace SubscriptionBilling.Application.Idempotency;

public interface IIdempotencyService
{
    Task<IdempotencyResult<TResponse>> ExecuteAsync<TResponse>(
        string key,
        string commandName,
        string requestHash,
        Func<CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken = default);
}
