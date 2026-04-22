using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Repositories;

public sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly BillingDbContext _dbContext;

    public SubscriptionRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
        => _dbContext.Subscriptions.AddAsync(subscription, cancellationToken).AsTask();

    public Task<Subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        => _dbContext.Subscriptions.FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

    public async Task<IReadOnlyList<Subscription>> GetDueActiveSubscriptionsAsync(DateTime asOfUtc, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Subscriptions
            .Where(x => x.Status == SubscriptionStatus.Active &&
                        x.NextBillingDateUtc.HasValue &&
                        x.NextBillingDateUtc.Value.Date <= asOfUtc.Date)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsActiveForCustomerPlanAsync(Guid customerId, string planCode, CancellationToken cancellationToken = default)
    {
        var normalizedPlanCode = planCode.Trim().ToUpperInvariant();
        return _dbContext.Subscriptions.AnyAsync(
            x => x.CustomerId == customerId &&
                 x.PlanCode == normalizedPlanCode &&
                 x.Status == SubscriptionStatus.Active,
            cancellationToken);
    }
}
