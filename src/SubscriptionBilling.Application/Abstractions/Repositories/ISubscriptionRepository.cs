using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Application.Abstractions.Repositories;

public interface ISubscriptionRepository
{
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetDueActiveSubscriptionsAsync(DateTime asOfUtc, CancellationToken cancellationToken = default);
    Task<bool> ExistsActiveForCustomerPlanAsync(Guid customerId, string planCode, CancellationToken cancellationToken = default);
}
