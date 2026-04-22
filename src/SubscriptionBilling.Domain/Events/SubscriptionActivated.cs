using SubscriptionBilling.Domain.Common;

namespace SubscriptionBilling.Domain.Events;

public sealed record SubscriptionActivated(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid SubscriptionId,
    Guid CustomerId) : IDomainEvent
{
    public static SubscriptionActivated Create(Guid subscriptionId, Guid customerId) =>
        new(Guid.NewGuid(), DateTime.UtcNow, subscriptionId, customerId);
}
