using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BillingDbContext _dbContext;

    public UnitOfWork(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
