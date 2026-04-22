using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Application.Idempotency;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Repositories;

public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly BillingDbContext _dbContext;

    public IdempotencyStore(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyRecordDto?> GetAsync(string key, string commandName, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key && x.CommandName == commandName, cancellationToken);

        return record is null
            ? null
            : new IdempotencyRecordDto(record.Id, record.Key, record.CommandName, record.RequestHash, record.ResponseJson, record.CreatedAtUtc);
    }

    public async Task SaveAsync(IdempotencyRecordDto record, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.IdempotencyRecords
            .AnyAsync(x => x.Key == record.Key && x.CommandName == record.CommandName, cancellationToken);

        if (exists)
        {
            return;
        }

        await _dbContext.IdempotencyRecords.AddAsync(new IdempotencyRecord
        {
            Id = record.Id,
            Key = record.Key,
            CommandName = record.CommandName,
            RequestHash = record.RequestHash,
            ResponseJson = record.ResponseJson,
            CreatedAtUtc = record.CreatedAtUtc
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
