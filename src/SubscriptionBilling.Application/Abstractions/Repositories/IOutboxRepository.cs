using SubscriptionBilling.Application.Outbox;

namespace SubscriptionBilling.Application.Abstractions.Repositories;

public interface IOutboxRepository
{
    Task<IReadOnlyList<OutboxMessageDto>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);
    Task MarkAttemptStartedAsync(Guid id, CancellationToken cancellationToken = default);
}
