using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Application.Outbox;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly BillingDbContext _dbContext;

    public OutboxRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OutboxMessageDto>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(x => x.ProcessedOnUtc == null)
            .OrderBy(x => x.OccurredOnUtc)
            .ThenBy(x => x.AttemptCount)
            .Take(batchSize)
            .Select(x => new OutboxMessageDto(x.Id, x.Type, x.Payload, x.OccurredOnUtc, x.ProcessedOnUtc, x.LastAttemptedOnUtc, x.AttemptCount, x.Error))
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAttemptStartedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FirstAsync(x => x.Id == id, cancellationToken);
        message.LastAttemptedOnUtc = DateTime.UtcNow;
        message.AttemptCount += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FirstAsync(x => x.Id == id, cancellationToken);
        message.ProcessedOnUtc = DateTime.UtcNow;
        message.Error = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages.FirstAsync(x => x.Id == id, cancellationToken);
        message.Error = error;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
