using SubscriptionBilling.Application.Idempotency;

namespace SubscriptionBilling.Application.Abstractions.Repositories;

public interface IIdempotencyStore
{
    Task<IdempotencyRecordDto?> GetAsync(string key, string commandName, CancellationToken cancellationToken = default);
    Task SaveAsync(IdempotencyRecordDto record, CancellationToken cancellationToken = default);
}
